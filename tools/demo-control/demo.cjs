
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync, spawn } = require('node:child_process');
const { createInterface } = require('node:readline');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const spec = require('./01_DATA_DEMO.json');
const { runtime, auth, personas, service, interaction, draftRegistration: draft } = spec;
const prepare = process.argv.includes('--prepare');
const rehearse = process.argv.includes('--rehearse');
const rehearseWpf = rehearse && process.argv.includes('--rehearse-wpf');
const fast = rehearse && process.argv.includes('--fast');
const offlineCheck = rehearse && process.argv.includes('--offline-check');
const api = new URL(runtime.apiHealthUrl);
const web = new URL(runtime.webUrl);
const log = (message) => {
  console.log(message);
  fs.appendFileSync(path.join(__dirname, 'demo.log'), new Date().toISOString() + ' ' + message + '\n');
};
const wait = (ms) => new Promise(resolve => setTimeout(resolve, ms));


async function checkApi() {
  const response = await fetch(api, { signal: AbortSignal.timeout(4500) });
  const body = await response.json();
  assert(response.ok && body.status === 'ok' && body.service === 'TiemChung.Api', 'API /health không hợp lệ');
}
async function checkRuntime() {
  try {
    const containers = execFileSync('docker', ['ps', '--filter', 'label=com.docker.compose.project=' + runtime.composeProject,
      '--format', '{{.Label "com.docker.compose.service"}}|{{.Status}}|{{.Ports}}'], {
      encoding: 'utf8', windowsHide: true, timeout: 9000, stdio: ['ignore', 'pipe', 'ignore'],
    }).trim().split(/\r?\n/);
    assert(containers.filter(row => /^sqlserver\|.*\(healthy\)/.test(row)).length === 1, 'SQL vac-final chưa healthy');
    assert(containers.filter(row => /^api\|.*127\.0\.0\.1:5080->/.test(row)).length === 1, 'API 5080 không thuộc vac-final');
    await checkApi();
    assert((await fetch(web, { signal: AbortSignal.timeout(4500) })).ok, 'Web 5173 chưa sẵn sàng');
  } catch (error) {
    throw new Error('Kiểm tra Docker vac-final, API 5080 và Web 5173. ' + (error.name === 'AssertionError' ? error.message : 'Runtime không phản hồi.'));
  }
}

function keyboard() {
  if (window.top !== window) return;
  let down = false;
  document.addEventListener('keyup', event => { if (event.key === 'F8') down = false; }, true);
  window.addEventListener('blur', () => { down = false; });
  document.addEventListener('keydown', event => {
    if (!['F8', 'F9', 'Escape'].includes(event.key) || event.isComposing || event.altKey || event.ctrlKey || event.metaKey) return;
    const editing = event.target instanceof Element &&
      event.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"])');
    if (editing && event.key === 'F8') return;
    if (event.key === 'Escape' && !window.klcnRunning && !window.klcnControlled) return;
    if (editing && !window.klcnRunning) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    if (event.repeat || (event.key === 'F8' && down)) return;
    if (event.key === 'F8') down = true;
    window.klcnKey(event.key).catch(() => {});
  }, true);
}

async function main() {
  assert(draft.submit === false && service.quantity === 1 && spec.guards.neverSubmitRegistration);
  await checkRuntime();
  // A temporary profile gives a fresh Customer session and keeps credentials out of browser storage.
  const browser = await chromium.launch({ channel: 'msedge', headless: false, args: ['--start-maximized', '--disable-save-password-bubble'] });
  let wpfWorker, wpfExit;
  try {
    const context = await browser.newContext({ viewport: null, serviceWorkers: 'block', acceptDownloads: false });
    let permitRecipient = false, permitRegistration = false, registrationAttempted = false, savedDemoId;
    let deniedWrites = 0;
    await context.route('**/*', async route => {
      const request = route.request();
      const url = new URL(request.url());
      if (url.origin !== api.origin || ['GET', 'HEAD', 'OPTIONS'].includes(request.method())) return route.continue();
      if (request.method() === 'POST' && /^\/api\/v1\/auth\/(login|refresh)$/.test(url.pathname)) return route.continue();
      if (permitRecipient && request.method() === 'POST' && url.pathname === '/api/v1/recipients') {
        permitRecipient = false;
        return route.continue();
      }
      if (permitRegistration && request.method() === 'POST' && url.pathname === '/api/v1/registrations') {
        const payload = request.postDataJSON();
        if (payload.customerId === 1 && payload.recipientId === 2 && payload.items?.length === 1 &&
          payload.items[0].kind === 'vaccine' && payload.items[0].quantity === 1) {
          permitRegistration = false;
          return route.continue();
        }
      }
      deniedWrites++;
      return route.abort();
    });
    const page = await context.newPage();
    page.setDefaultTimeout(11000);
    let phase = 'SETUP', pausedFrom, index = 0, lastF8 = 0, stageError, runtimePrice;
    let latestRegistrationRead, writesSent = 0, recipientPosts = 0, apiFailure = false;
    page.on('requestfailed', request => {
      if (new URL(request.url()).origin === api.origin && !request.failure()?.errorText.includes('ERR_ABORTED')) apiFailure = true;
    });
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin !== api.origin || request.method() !== 'POST') return;
      if (url.pathname === '/api/v1/recipients') recipientPosts++;
      if (url.pathname === '/api/v1/registrations') writesSent++;
    });
    page.on('response', response => {
      if (response.ok() && response.request().method() === 'GET' && new URL(response.url()).pathname === '/api/v1/registrations') latestRegistrationRead = response;
    });
    const stages = ['Trang chủ', 'Danh mục Avaxim', 'Hồ sơ phụ huynh và bé', 'Chọn dịch vụ', 'Liên hệ và thời gian', 'Xem lại', 'Windows WPF'];
    async function status(message) {
      await page.evaluate(({ phase, message }) => {
        window.klcnRunning = ['RUNNING', 'PAUSED'].includes(phase);
        window.klcnControlled = ['READY', 'RUNNING', 'PAUSED', 'NATIVE'].includes(phase);
        const controls = window.klcnControls;
        if (controls && controls.querySelector('[data-action="write"]') && phase !== 'DONE' && phase !== 'READY') {
          controls.querySelector('[data-action="write"]').hidden = true;
        }
        if (controls) (document.querySelector('dialog[open]') || document.body).append(controls);
        let panel = document.getElementById('klcn-demo-state');
        if (!panel) {
          panel = document.createElement('div');
          panel.id = 'klcn-demo-state';
          panel.setAttribute('role', 'status');
          Object.assign(panel.style, {
            position: 'fixed', bottom: '14px', right: '14px', zIndex: '2147483647', pointerEvents: 'none',
            background: '#12345b', color: 'white', padding: '9px 14px', font: '14px Segoe UI, sans-serif',
            borderRadius: '7px', maxWidth: '44vw',
          });
          document.body.append(panel);
        }
        (document.querySelector('dialog[open]') || document.body).append(panel);
        panel.textContent = phase + ' · ' + message;
      }, { phase, message });
    }
    let runGeneration = 0, webOnly = false, catalogOnly = false;
    async function gate() {
      while (phase === 'PAUSED') await wait(50);
      if (phase === 'MANUAL' || phase === 'MENU') throw new Error('Đã dừng');
    }
    async function pace(ms) {
      if (!fast) await wait(ms);
      await gate();
    }
    const btn = (name, scope = page) => scope.getByRole('button', { name, exact: true });
    const heading = (name, scope = page) => scope.getByRole('heading', { name, exact: true });
    const field = (name, scope = page) => scope.getByLabel(name, { exact: true });
    async function click(locator) {
      await locator.scrollIntoViewIfNeeded();
      const box = await locator.boundingBox();
      await gate();
      if (box) await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 10 });
      await gate();
      await locator.click({ timeout: 5000 });
      await pace(interaction.afterClickMs);
    }
    async function type(locator, value) {
      await click(locator);
      await locator.fill('');
      for (const character of value) {
        await gate();
        await page.keyboard.insertText(character);
        await pace(120 + Math.floor(Math.random() * 41));
      }
      assert.equal(await locator.inputValue(), value, 'Giá trị ô nhập không khớp');
      await pace(interaction.afterFieldMs);
    }
    async function route(hash) {
      await gate();
      await page.evaluate(hash => { location.hash = hash; }, hash);
      await page.waitForURL(url => url.hash === '#' + hash);
      await pace(interaction.afterClickMs);
    }
    const child = personas.recipient;
    const birth = child.dateOfBirth.split('-').reverse().join('/');
    const childHeading = heading(child.fullName);
    const card = page.locator('.profile-recipient-card').filter({ has: childHeading });
    const ownerPanel = page.locator('[aria-labelledby="personal-title"]');
    async function openProfiles() {
      await route('account');
      await ownerPanel.getByText(personas.customer.fullName, { exact: true }).waitFor();
      await ownerPanel.getByText(personas.customer.phoneNumber, { exact: true }).waitFor();
      await ownerPanel.getByText(personas.customer.dateOfBirth.split('-').reverse().join('/'), { exact: true }).waitFor();
    }
    async function verifyChild() {
      await childHeading.waitFor();
      assert.equal(await card.count(), 1, 'Có nhiều hồ sơ trùng tên của bé');
      await card.getByText(child.relationshipToCustomer + ' · ' + birth, { exact: true }).waitFor();
    }

    const actions = [
      async () => {
        await route('home');
        await page.locator('main').waitFor();
        await page.evaluate(() => window.scrollBy({ top: 220, behavior: 'smooth' }));
      },
      async () => {
        await route('catalog');
        const search = field('Tìm theo mã, tên, hãng hoặc phòng bệnh');
        await type(search, service.name.split(' ')[0]);
        await click(btn('Tìm kiếm'));
        const response = await fetch(new URL('/api/v1/catalog/vaccines?search=' + encodeURIComponent(service.code), api), {
          signal: AbortSignal.timeout(4500),
        });
        const catalog = await response.json();
        const item = catalog.items?.find(item => item.code === service.code && item.name === service.name);
        assert(response.ok && catalog.source === 'database' && item && Number.isSafeInteger(item.price) && item.price > 0,
          'Không xác nhận được mã/giá vaccine từ danh mục SQL');
        runtimePrice = item.price;
        const row = page.getByRole('row').filter({ hasText: service.code });
        await click(row.getByRole('link', { name: 'Xem chi tiết ' + service.name, exact: true }));
        await heading(service.name).waitFor();
        await heading('Mô tả').waitFor();
      },
      async () => {
        await openProfiles();
        await verifyChild();
        await click(btn('Xem ' + child.fullName, card));
        await page.getByRole('dialog').getByText(child.gender, { exact: true }).waitFor();
      },
      async () => {
        const profileDialog = page.getByRole('dialog');
        if (await profileDialog.isVisible()) await click(btn('Đóng', profileDialog));
        await route('register');
        const saveDraft = btn('Lưu bản nháp trong phiên, về danh sách');
        if (await saveDraft.isVisible()) await click(saveDraft);
        const list = btn('Danh sách đăng ký');
        if (await list.isVisible()) await click(list);
        const discard = btn('Bỏ bản nháp');
        if (await discard.isVisible()) {
          const accept = dialog => dialog.type() === 'confirm' && dialog.message().startsWith('Bỏ bản nháp chưa gửi')
            ? dialog.accept() : dialog.dismiss();
          page.once('dialog', accept);
          try { await click(discard); } finally { page.off('dialog', accept); }
        }
        await click(btn('Tạo đăng ký'));
        const select = page.getByRole('combobox', { name: /^Người được tiêm/ });
        const childOption = child.fullName + ' · ' + birth + ' · ' + child.relationshipToCustomer;
        await select.selectOption({ label: childOption });
        await pace(interaction.afterClickMs);
        await click(btn('Chọn dịch vụ'));
        await type(field('Tìm dịch vụ'), service.name.split(' ')[0]);
        await click(btn('Chọn ' + service.name));
        assert.equal(await field('Số lượng ' + service.name).inputValue(), String(service.quantity));
        const subtotal = await page.locator('.registration-summary .registration-total strong').innerText();
        assert.equal(Number(subtotal.replace(/\D/g, '')), runtimePrice, 'Tạm tính không khớp giá từ API');
      },
      async () => {
        await click(btn('Nhập thông tin liên hệ'));
        for (const [label, key] of [
          ['Họ tên liên hệ', 'fullName'], ['Số điện thoại', 'phoneNumber'], ['Email', 'email'],
          ['Tỉnh / Thành phố', 'provinceCity'], ['Phường / Xã', 'wardCommune'], ['Địa chỉ', 'addressLine'],
        ]) await type(field(label + ' *'), draft.contact[key]);
        const date = field('Ngày mong muốn tiêm *');
        const time = field('Giờ mong muốn (giờ Việt Nam) *');
        for (const [control, value] of [[date, draft.preferredDate], [time, draft.preferredTime]]) {
          await click(control);
          await control.fill(value);
          assert(await control.evaluate(input => input.validity.valid), 'Ngày/giờ mong muốn không hợp lệ');
          await pace(interaction.afterFieldMs);
        }
      },
      async () => {
        await click(btn('Xem lại'));
        await heading('Kiểm tra trước khi xác nhận').waitFor();
        const review = page.locator('.registration-panel').filter({ has: heading('Kiểm tra trước khi xác nhận') });
        for (const value of [...Object.values(draft.contact), child.fullName, service.name, 'Số lượng: ' + service.quantity]) {
          await review.getByText(value, { exact: true }).first().waitFor();
        }
        for (const value of [draft.preferredDate.split('-').reverse().join('/'), draft.preferredTime]) {
          assert((await review.innerText()).includes(value), 'Ngày/giờ ở bước xem lại không khớp');
        }
        assert.equal(await review.getByRole('combobox', { name: /^Ưu đãi/ }).inputValue(), '');
        const amount = (text) => Number(text.replace(/\D/g, ''));
        const feeLabel = review.getByText('Phí khám/tư vấn', { exact: true }).locator('..').locator('dd');
        const total = review.locator('.registration-total strong');
        assert.equal(amount(await total.innerText()), runtimePrice + amount(await feeLabel.innerText()),
          'Tổng thanh toán không khớp giá và phí trên UI');
        await btn('Xác nhận đăng ký').waitFor();
        // Preserve manual review while preventing accidental confirmation in the demo window.
        await btn('Xác nhận đăng ký').evaluate(button => { button.disabled = true; });
        await total.scrollIntoViewIfNeeded();
      },
      async () => {
        const args = ['-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File',
          path.join(__dirname, 'WpfDemo.ps1'), '-OwnerPid', String(process.pid)];
        if (rehearseWpf) args.push('-Rehearse');
        wpfWorker = spawn('powershell.exe', args, { windowsHide: true, stdio: ['ignore', 'pipe', 'ignore'] });
        let stageSevenPassed = false;
        let failure = 'Không mở được điều khiển WPF. Dùng Alt+Tab và thao tác thủ công.';
        wpfExit = new Promise(resolve => {
          wpfWorker.once('error', () => resolve(1));
          wpfWorker.once('close', resolve);
        });
        await new Promise((resolve, reject) => {
          const lines = createInterface({ input: wpfWorker.stdout });
          lines.on('line', line => {
            log(line);
            if (line === 'WAIT_FOCUS') void status('Alt+Tab sang WPF · sau đó dùng F8 trong WPF').catch(() => {});
            if (line.startsWith('PASS F8 #7')) { stageSevenPassed = true; resolve(); }
            if (line.startsWith('BLOCKED')) failure = line.split(' — ').slice(1).join(' — ') || failure;
            if (line === 'MANUAL WPF — Esc') phase = 'MANUAL';
            if (line === 'MENU WPF') void returnToMenu('Đã dừng WPF').catch(() => {});
          });
          void wpfExit.then(code => {
            if (!stageSevenPassed) reject(new Error(failure));
            else if (code) void status('WPF lỗi · dùng Alt+Tab rồi thao tác tay').catch(() => {});
          });
        });
      },
    ];

    async function onKey(key) {
      if (key === 'Escape' && ['READY', 'RUNNING', 'PAUSED'].includes(phase)) {
        phase = 'MANUAL';
        wpfWorker?.kill();
        log('MANUAL — Esc');
        await status('Thao tác thủ công · chặn gửi đăng ký');
        return;
      }
      if (key === 'F9' && ['READY', 'RUNNING', 'PAUSED'].includes(phase)) {
        if (phase === 'PAUSED') phase = pausedFrom;
        else { pausedFrom = phase; phase = 'PAUSED'; }
        log(phase + ' — F9');
        await status(stages[index]);
        return;
      }
      if (key !== 'F8' || phase !== 'READY' || index >= (webOnly ? 6 : stages.length) || Date.now() - lastF8 < 700) return;
      lastF8 = Date.now();
      apiFailure = false;
      phase = 'RUNNING';
      const generation = runGeneration;
      await status((index + 1) + '/10 ' + stages[index]);
      try {
        try { await checkApi(); } catch { throw new Error('API vac-final không phản hồi hoặc sai health'); }
        await gate();
        await actions[index]();
        await gate();
        if (generation !== runGeneration) return;
        await page.evaluate(() => document.activeElement?.blur());
        if (index < 6) log('PASS F8 #' + (index + 1) + ' — ' + stages[index]);
        index++;
        phase = index < (catalogOnly ? 2 : webOnly ? 6 : stages.length) ? 'READY' : 'DONE';
        await status(catalogOnly ? 'READ · Danh mục Avaxim' : webOnly && index === 6 ? 'PREVIEW · Xem lại đăng ký người được tiêm demo' : index < stages.length ? 'F8 · ' + (index + 1) + '/10 ' + stages[index] : 'WPF · tiếp tục F8 #8–#10 trong cửa sổ WPF');
        if (index === 6 && !registrationAttempted) {
          await page.evaluate(() => { window.klcnControls.querySelector('[data-action="write"]').hidden = false; });
        }
        if (rehearse && index === 6) await page.screenshot({ path: path.join(__dirname, '06-xem-lai.png'), fullPage: true });
      } catch (error) {
        if (generation !== runGeneration || phase === 'MANUAL' || phase === 'MENU') return;
        phase = 'BLOCKED';
        stageError = apiFailure ? new Error('Edge không kết nối được API vac-final') : error;
        const message = stageError.message.split('\n')[0];
        log('BLOCKED F8 #' + (index + 1) + ' — ' + message);
        await status(stages[index] + ' · ' + message + ' · thao tác tay');
      }
    }
    let activeStage = null;

    const sceneLabels = {
      profiles: 'Hồ sơ khách hàng và đăng ký #1',
      payment: 'Thanh toán mô phỏng #1',
      appointment: 'Lịch hẹn #1 và nhắc lịch',
      clinical: 'Khám sàng lọc và chỉ định đã lưu',
      nurse: 'Mũi tiêm và theo dõi đã lưu',
      warehouse: 'Kho vắc xin và nhập kho',
      catalog: 'Danh mục và khuyến mãi',
      reports: 'Báo cáo',
      accounts: 'Tài khoản và phân quyền',
    };
    let unified = false;
    let nativeScene = null;
    let returning = false;
    async function runNativeScene(scene, generation) {
      const args = ['-NoProfile', '-STA', '-ExecutionPolicy', 'Bypass', '-File',
        path.join(__dirname, 'WpfDemo.ps1'), '-OwnerPid', String(process.pid), '-Scene', scene];
      if (scene === 'profiles' && savedDemoId) args.push('-RegistrationId', String(savedDemoId));
      let switched = false;
      while (generation === runGeneration) {
        const worker = spawn('powershell.exe', args, { windowsHide: true, stdio: ['ignore', 'pipe', 'ignore'] });
        wpfWorker = worker;
        let nextScene = false;
        let back = false;
        let failure;
        createInterface({ input: worker.stdout }).on('line', line => {
          log(line);
          if (line.startsWith('BLOCKED')) failure = line;
          if (line === 'NEXT WPF') nextScene = true;
          if (line === 'MENU WPF') back = true;
          if (line === 'WAIT_FOCUS') void status('Alt+Tab sang cửa sổ WPF đã mở').catch(() => {});
        });
        const code = await new Promise(resolve => {
          worker.once('error', () => resolve(1));
          worker.once('close', resolve);
        });
        if (wpfWorker === worker) wpfWorker = null;
        if (generation !== runGeneration) return 'cancelled';
        if (code === 12 && !switched) {
          switched = true;
          await wait(1000);
          continue;
        }
        if (back) return 'menu';
        if (code) throw new Error(failure || 'Không mở được cảnh WPF');
        return nextScene ? 'next' : 'menu';
      }
      return 'cancelled';
    }
    async function returnToMenu(message) {
      if (returning) return;
      returning = true;
      try {
        runGeneration++;
        phase = 'MENU';
        unified = false;
        wpfWorker?.kill();
        await Promise.allSettled([activeStage, nativeScene]);
        await page.bringToFront();
        await status(message);
        await showMenu(true);
      } finally { returning = false; }
    }
    async function openNativeScenes(scenes, thenWeb = false) {
      if (returning || nativeScene) return;
      const generation = runGeneration;
      phase = 'NATIVE';
      await showMenu(false);
      let message = 'Đã kết thúc cảnh';
      let completed = false;
      const task = (async () => {
        await checkApi();
        for (const scene of scenes) {
          if (generation !== runGeneration) return;
          await status('WPF · ' + sceneLabels[scene]);
          if (await runNativeScene(scene, generation) !== 'next') return;
        }
        completed = true;
      })();
      nativeScene = task;
      try { await task; } catch (error) {
        message = String(error.message).replace(/[\r\n].*/, '');
        log(message);
      } finally {
        nativeScene = null;
      }
      if (generation !== runGeneration) return;
      if (thenWeb && (completed || message.startsWith('BLOCKED WPF'))) {
        await page.bringToFront();
        phase = 'READY';
        await route('home');
        await status(completed ? 'F8 · 1/10 Trang chủ' : 'WPF chưa sẵn sàng · F8 bắt đầu phần Web');
      } else await returnToMenu(message);
    }
    const originalOnKey = onKey;
    async function runKey(key) {
      if (key === 'Escape' && phase === 'NATIVE') { await returnToMenu('Đã dừng WPF'); return; }
      if (returning || (activeStage && key === 'F8')) return;
      if (key === 'F8' && unified && index === 6 && phase === 'READY') {
        await openNativeScenes(['profiles', 'payment', 'appointment', 'clinical', 'nurse', 'reports', 'accounts']);
        return;
      }
      const task = originalOnKey(key);
      if (key === 'F8') activeStage = task;
      try { await task; } finally { if (activeStage === task) activeStage = null; }
    }
    async function submitDemoRegistration() {
      if (registrationAttempted || index !== 6 || !['READY', 'DONE'].includes(phase)) return;
      const confirm = btn('Xác nhận đăng ký');
      if (!await confirm.isVisible()) return;
      const approved = await page.evaluate(() => window.confirm('Ghi thật một đăng ký mới cho người được tiêm demo vào TiemChung? Có thể khôi phục bằng shortcut RESET riêng.'));
      if (!approved || registrationAttempted || !['READY', 'DONE'].includes(phase)) return;
      registrationAttempted = true;
      const previousPhase = phase;
      phase = 'WRITING';
      await status('WRITE · Đang gửi đăng ký người được tiêm demo');
      try {
        const posted = page.waitForResponse(response => new URL(response.url()).origin === api.origin &&
          new URL(response.url()).pathname === '/api/v1/registrations' && response.request().method() === 'POST',
          { timeout: 12000 });
        permitRegistration = true;
        await confirm.evaluate(button => { button.disabled = false; });
        const [response] = await Promise.all([posted, confirm.click()]);
        assert.equal(response.status(), 201, 'API không xác nhận tạo đăng ký');
        const saved = await response.json();
        assert(Number.isSafeInteger(saved.id) && saved.id > 1 && saved.recipient?.id === 2);
        savedDemoId = saved.id;
        await page.getByText(new RegExp('Đã tạo đăng ký DK-' + String(saved.id).padStart(4, '0'))).first().waitFor();
        phase = previousPhase;
        await status('WRITE PASS · đăng ký người được tiêm demo #' + savedDemoId + ' · #1 là hồ sơ đã hoàn thành');
        log('PASS WRITE đăng ký người được tiêm demo #' + savedDemoId);
      } catch (error) {
        phase = 'BLOCKED';
        await status('Kiểm tra danh sách trước khi tiếp tục · kết quả ghi chưa xác nhận');
        log('BLOCKED WRITE · kiểm tra danh sách, không gửi lại tự động');
      } finally {
        permitRegistration = false;
        if (await confirm.isVisible()) await confirm.evaluate(button => { button.disabled = true; });
      }
    }
    async function menuAction(action) {
      if (phase === 'WRITING') return;
      if (action === 'write') { await submitDemoRegistration(); return; }
      if (action === 'pause') { await runKey('F9'); return; }
      if (action === 'stop' || action === 'menu') { await returnToMenu('Đã dừng · chọn cảnh'); return; }
      if (returning || nativeScene || activeStage) return;
      if (action.startsWith('wpf:') && Object.hasOwn(sceneLabels, action.slice(4))) {
        await openNativeScenes([action.slice(4)]);
        return;
      }
      if (action === 'full' || action === 'web') {
        unified = action === 'full';
        webOnly = action === 'web';
        catalogOnly = false;
        runGeneration++;
        phase = 'READY';
        index = 0;
        lastF8 = 0;
        await showMenu(false);
        if (unified) { await openNativeScenes(['catalog', 'warehouse'], true); return; }
        await route('home');
        await status('F8 · 1/10 Trang chủ');
        return;
      }
      if (action === 'catalog') {
        unified = false;
        webOnly = false;
        catalogOnly = true;
        runGeneration++;
        phase = 'READY';
        index = 1;
        lastF8 = 0;
        await showMenu(false);
        await runKey('F8');
        return;
      }
    }
    async function showMenu(visible) {
      await page.evaluate(visible => {
        window.klcnControls.querySelector('#klcn-menu').hidden = !visible;
      }, visible);
    }
    await page.exposeFunction('klcnKey', runKey);
    await page.exposeFunction('klcnMenuAction', menuAction);
    await page.addInitScript(keyboard);
    await page.goto(web.origin + '/#account');
    const password = fs.readFileSync(path.resolve(runtime.repository, auth.credentialsSourceRelativePath), 'utf8')
      .split(/\r?\n/).map(line => [...line.matchAll(/\x60([^\x60]+)\x60/g)].map(match => match[1]))
      .find(fields => fields[0] === auth.customerUsername)?.[1];
    assert(password, 'Không đọc được dòng tài khoản demo Customer');
    try {
      await field('Email').fill(auth.customerUsername);
      await field('Mật khẩu').fill(password);
      await click(btn('Đăng nhập', page.locator('form')));
      await btn('Đăng xuất').waitFor();
    } catch {
      throw new Error('Không đăng nhập được Customer demo qua Web');
    }
    await openProfiles();
    if (prepare) {
      await btn('Thêm mới').waitFor();
      assert.equal(await page.locator('.profile-feedback.error').count(), 0, 'Không xác minh được danh sách người được tiêm');
      if (await card.count() === 0) {
        await click(btn('Thêm mới'));
        const dialog = page.getByRole('dialog');
        await dialog.getByText(personas.customer.fullName, { exact: true }).waitFor();
        await field('Họ và tên *', dialog).fill(child.fullName);
        await field('Ngày sinh *', dialog).fill(child.dateOfBirth);
        await dialog.getByRole('combobox', { name: /^Giới tính/ }).selectOption({ label: child.gender });
        await dialog.getByRole('combobox', { name: /^Quan hệ với khách hàng/ }).selectOption({ label: child.relationshipToCustomer });
        const saved = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/recipients' && response.request().method() === 'POST');
        permitRecipient = true;
        try {
          await click(btn('Lưu hồ sơ', dialog));
          assert((await saved).ok(), 'API từ chối lưu hồ sơ bé');
          await verifyChild();
        } finally { permitRecipient = false; }
        await click(btn('Tải lại'));
        await verifyChild();
        assert.equal(recipientPosts, 1);
        log('PASS chuẩn bị — tạo qua UI đúng một lần và đọc lại hồ sơ bé');
        return;
      }
      await verifyChild();
      log('PASS chuẩn bị — hồ sơ bé đã tồn tại; không tạo thêm');
      return;
    }
    await verifyChild();
    if (rehearse) {
      await route('register');
      await btn('Tạo đăng ký').waitFor();
      for (let count = 0; !latestRegistrationRead && count < 80; count++) await wait(100);
      assert(latestRegistrationRead, 'Không đọc được danh sách đăng ký trước demo');
    }
    const before = await latestRegistrationRead?.json();
    await route('home');
    phase = 'READY';
    await page.evaluate(labels => {
      const container = document.createElement('div');
      container.id = 'klcn-controls';
      container.innerHTML = `
        <style>
        #klcn-controls { position:fixed; z-index:2147483647; bottom:12px; left:12px; font:14px Segoe UI,sans-serif; color:#fff }
        #klcn-controls button { padding:8px 10px; margin:3px; cursor:pointer; border:1px solid #c4d3e8; background:#fff; color:#173052; border-radius:5px }
        #klcn-menu { position:absolute; left:0; bottom:100%; max-height:75vh; overflow:auto; background:#12345b; padding:16px; border-radius:8px; width:340px; box-shadow:0 4px 18px #0005 }
        #klcn-menu h2 { font-size:18px; margin:0 0 8px; color:#fff }
        #klcn-menu p { margin:6px 0; color:#fff }
        #klcn-menu [data-action] { display:block; width:100%; text-align:left }
        </style>
        <button type="button" data-action="menu">Quay về menu</button>
        <button type="button" data-action="pause">Pause / Resume</button>
        <button type="button" data-action="stop">Stop</button>
        <button type="button" data-action="write" hidden>Ghi đăng ký người được tiêm demo…</button>
        <div id="klcn-menu" hidden>
          <h2>KLCN DEMO</h2><p>F8 từng bước · F9 tạm dừng · Esc thao tác tay</p>
          <button data-action="full">Chạy toàn bộ · Web → WPF</button>
          <button data-action="catalog">Web · Danh mục Avaxim (chỉ xem)</button>
          <p>WPF · mở cửa sổ trước khi trình diễn</p>
          <button data-action="web">Web · Đăng ký người được tiêm demo (PREVIEW)</button>
          <p>WPF đọc hồ sơ đã lưu. Đăng ký #1 là người được tiêm lịch sử.</p>
        </div>`;
      container.addEventListener('click', event => {
        const action = event.target.closest('[data-action]')?.dataset.action;
        if (action) window.klcnMenuAction(action).catch(() => {});
      });
      for (const [id, label] of Object.entries(labels)) {
        const button = document.createElement('button');
        button.type = 'button';
        button.dataset.action = 'wpf:' + id;
        button.textContent = label;
        container.querySelector('#klcn-menu').append(button);
      }
      window.klcnControls = container;
      document.body.append(container);
      new MutationObserver(() => {
        if (!container.isConnected) document.body.append(container);
      }).observe(document.body, { childList: true, subtree: true });
    }, sceneLabels);
    await status('F8 · 1/10 Trang chủ · F9 tạm dừng · Esc thao tác tay');
    log('READY — Edge / vac-final / hồ sơ người được tiêm demo đã kiểm tra');
    if (!rehearse) await showMenu(true);
    if (rehearse) {
      await page.keyboard.press('F9');
      await page.waitForFunction(() => document.getElementById('klcn-demo-state')?.textContent.startsWith('PAUSED'));
      await page.keyboard.press('F8');
      assert.equal(index, 0, 'F8 phải bị bỏ qua khi tạm dừng');
      await page.keyboard.press('F9');
      await page.waitForFunction(() => document.getElementById('klcn-demo-state')?.textContent.startsWith('READY'));
      await page.keyboard.down('F8');
      await page.keyboard.down('F8');
      await page.keyboard.up('F8');
      for (let expected = 1; expected <= (rehearseWpf ? stages.length : 6); expected++) {
        if (expected > 1) {
          await wait(720);
          if (offlineCheck && expected === 2) await context.setOffline(true);
          await page.keyboard.press('F8');
        }
        const deadline = Date.now() + 90000;
        while (index < expected && !stageError && Date.now() < deadline) await wait(75);
        if (offlineCheck && expected === 2) {
          assert.equal(stageError?.message, 'Edge không kết nối được API vac-final');
          assert.equal(index, 1);
          log('PASS mất kết nối API trong riêng cửa sổ kiểm thử · dừng ở F8 #2');
          return;
        }
        if (stageError) throw stageError;
        assert.equal(index, expected);
      }
      if (rehearseWpf) assert.equal(await wpfExit, 0, 'Tổng duyệt WPF thất bại');
      assert.equal(writesSent, 0);
      assert.equal(recipientPosts, 0);
      assert.equal(deniedWrites, 0);
      await page.bringToFront();
      const reload = page.waitForResponse(response => response.ok() && response.request().method() === 'GET' && new URL(response.url()).pathname === '/api/v1/registrations');
      await route('register');
      await click(btn('Lưu bản nháp trong phiên, về danh sách'));
      const after = await (await reload).json();
      assert.deepEqual(after, before);
      log('PASS đối chiếu GET danh sách đăng ký trước/sau · 0 POST đăng ký/hồ sơ trong tổng duyệt');
      await page.screenshot({ path: path.join(__dirname, 'tong-duyet-danh-sach.png') });
      phase = 'READY';
      await status('Kiểm thử Esc');
      await page.keyboard.press('Escape');
      await page.waitForFunction(() => document.getElementById('klcn-demo-state')?.textContent.startsWith('MANUAL'));
      await page.keyboard.press('F8');
      assert.equal(phase, 'MANUAL');
      log('PASS giữ F8, F9 tạm dừng/tiếp tục và Esc');
      return;
    }
    await new Promise(resolve => { page.once('close', resolve); browser.once('disconnected', resolve); });
  } finally {
    wpfWorker?.kill();
    await browser.close();
  }
}

main().catch(async error => {
  log('BLOCKED — ' + error.message.split('\n')[0]);
  if (rehearse || prepare) process.exitCode = 1;
  else await new Promise(resolve => process.stdin.once('data', resolve).resume());
});
