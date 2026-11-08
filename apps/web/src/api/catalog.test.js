import test from 'node:test'
import assert from 'node:assert/strict'
import { getVaccine, getVaccines, getPackages, getPackage, getDiseases, getVaccineTechnologies, getRegistrationCatalog } from './client.js'
import { money } from '../registrationForm.js'

test('catalog sends encoded search and technology/disease/price filters to the public API', async () => {
  const calls = []
  const original = globalThis.fetch
  globalThis.fetch = async url => { calls.push(new URL(url)); return Response.json({ source: 'database', items: [] }) }
  try {
    await getVaccines('A & B', { status: 'Active', technologyId: 2, diseaseId: '', minPrice: 0, maxPrice: 250000 })
    assert.equal(calls[0].pathname, '/api/v1/catalog/vaccines')
    assert.equal(calls[0].searchParams.get('search'), 'A & B')
    assert.equal(calls[0].searchParams.get('status'), 'Active')
    assert.equal(calls[0].searchParams.get('technologyId'), '2')
    assert.equal(calls[0].searchParams.get('minPrice'), '0')
    assert.equal(calls[0].searchParams.get('maxPrice'), '250000')
    assert.equal(calls[0].searchParams.has('injectionTypeId'), false)
    await getPackages('gói', { status: 'Active', technologyId: 1, diseaseId: 3, minPrice: 10, maxPrice: 500000 })
    assert.equal(calls[1].pathname, '/api/v1/catalog/packages')
    assert.equal(calls[1].searchParams.get('search'), 'gói')
    assert.equal(calls[1].searchParams.get('status'), 'Active')
    assert.equal(calls[1].searchParams.get('technologyId'), '1')
    assert.equal(calls[1].searchParams.get('diseaseId'), '3')
    assert.equal(calls[1].searchParams.get('minPrice'), '10')
    assert.equal(calls[1].searchParams.get('maxPrice'), '500000')
    await getDiseases()
    await getVaccineTechnologies()
    assert.deepEqual(calls.slice(2).map(x => [x.pathname, x.searchParams.get('status')]), [
      ['/api/v1/catalog/diseases', 'Active'],
      ['/api/v1/catalog/vaccine-technologies', 'Active'],
    ])
  } finally { globalThis.fetch = original }
})

test('catalog surfaces HTTP failures', async () => {
  const original = globalThis.fetch
  globalThis.fetch = async () => new Response(null, { status: 503 })
  try { await assert.rejects(getVaccines(), /HTTP 503/) } finally { globalThis.fetch = original }
})

test('vaccine detail uses the detail endpoint and preserves exact money', async () => {
  const original = globalThis.fetch
  const vaccine = { id: 12, name: '[DEMO] Same name', manufacturer: 'Test', origin: 'Test', price: '9999999999999999' }
  const calls = []
  globalThis.fetch = async url => {
    const request = new URL(url)
    calls.push(request.pathname)
    if (request.pathname === '/api/v1/catalog/vaccines/12') {
      return new Response('{"id":12,"name":"[DEMO] Same name","manufacturer":"Test","origin":"Test","price":9999999999999999.00}')
    }
    assert.fail(`Unexpected request: ${request.pathname}`)
  }
  try {
    assert.deepEqual(await getVaccine('12'), vaccine)
    assert.deepEqual(calls, ['/api/v1/catalog/vaccines/12'])
  } finally { globalThis.fetch = original }
})

test('vaccine and package detail reject invalid IDs without fetching', async () => {
  const original = globalThis.fetch
  globalThis.fetch = () => { assert.fail('Invalid IDs must not request a catalog') }
  try {
    for (const getDetail of [getVaccine, getPackage]) {
      for (const id of ['', '0', '-1', '1.2', '12abc', '9007199254740992', '1/2']) {
        assert.equal(await getDetail(id), null)
      }
    }
  } finally { globalThis.fetch = original }
})

test('vaccine and package detail return null on 404 and propagate other failures', async () => {
  const original = globalThis.fetch
  try {
    globalThis.fetch = async () => new Response(null, { status: 404 })
    assert.equal(await getVaccine('1'), null)
    assert.equal(await getPackage('1'), null)
    globalThis.fetch = async () => new Response(null, { status: 503 })
    await assert.rejects(getVaccine('1'), /HTTP 503/)
    await assert.rejects(getPackage('1'), /HTTP 503/)
    const networkError = new Error('network unavailable')
    globalThis.fetch = async () => { throw networkError }
    await assert.rejects(getVaccine('1'), error => error === networkError)
  } finally { globalThis.fetch = original }
})

test('package detail uses the detail endpoint and preserves exact money', async () => {
  const original = globalThis.fetch
  globalThis.fetch = async url => {
    assert.equal(new URL(url).pathname, '/api/v1/catalog/packages/23')
    return new Response('{"id":23,"name":"Gói thử","price":9999999999999999.00}')
  }
  try {
    assert.deepEqual(await getPackage(23), { id: 23, name: 'Gói thử', price: '9999999999999999' })
  } finally { globalThis.fetch = original }
})

for (const [rawPrice, exactPrice, display] of [
  ['9999999999999999.00', '9999999999999999', '9.999.999.999.999.999 đ'],
  ['250000.00', '250000', '250.000 đ'],
  ['0.00', '0', '0 đ'],
]) {
  test(`catalog and G2 preserve and format raw JSON price ${rawPrice} exactly`, async () => {
    const original = globalThis.fetch
    // Keep decimal tokens in the response body; constructing a Number would lose precision.
    globalThis.fetch = async url => {
      const request = new URL(url)
      const item = `{"id":12,"name":"[DEMO] Test","price":${rawPrice},"items":[{"vaccineId":12,"quantity":1}]}`
      if (request.pathname === '/api/v1/catalog/vaccines/12') return new Response(item)
      return new Response(`{"source":"database","items":[${item}]}`)
    }
    try {
      const vaccines = await getVaccines()
      const packages = await getPackages()
      const detail = await getVaccine('12')
      const registration = await getRegistrationCatalog()
      for (const item of [vaccines.items[0], packages.items[0], detail, ...registration]) {
        assert.equal(item.price, exactPrice)
        assert.equal(money(item.price), display)
      }
      assert.equal(vaccines.items[0].id, 12)
      assert.deepEqual(packages.items[0].items, [{ vaccineId: 12, quantity: 1 }])
    } finally { globalThis.fetch = original }
  })
}
