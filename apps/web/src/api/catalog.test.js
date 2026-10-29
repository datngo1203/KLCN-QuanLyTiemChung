import test from 'node:test'
import assert from 'node:assert/strict'
import { getVaccine, getVaccines, getPackages, getVaccineTypes, getInjectionTypes } from './client.js'

test('catalog sends encoded search and type/price filters to the public API', async () => {
  const calls = []
  const original = globalThis.fetch
  globalThis.fetch = async url => { calls.push(new URL(url)); return Response.json({ source: 'database', items: [] }) }
  try {
    await getVaccines('A & B', { vaccineTypeId: 2, injectionTypeId: '', minPrice: 0, maxPrice: 250000 })
    assert.equal(calls[0].pathname, '/api/v1/catalog/vaccines')
    assert.equal(calls[0].searchParams.get('search'), 'A & B')
    assert.equal(calls[0].searchParams.get('vaccineTypeId'), '2')
    assert.equal(calls[0].searchParams.get('minPrice'), '0')
    assert.equal(calls[0].searchParams.get('maxPrice'), '250000')
    assert.equal(calls[0].searchParams.has('injectionTypeId'), false)
    await getPackages('gói', { injectionTypeId: 1 })
    assert.equal(calls[1].pathname, '/api/v1/catalog/packages')
    assert.equal(calls[1].searchParams.get('injectionTypeId'), '1')
    await getVaccineTypes(); await getInjectionTypes()
    assert.deepEqual(calls.slice(2).map(x => x.pathname), ['/api/v1/catalog/vaccine-types', '/api/v1/catalog/injection-types'])
  } finally { globalThis.fetch = original }
})

test('catalog surfaces HTTP failures', async () => {
  const original = globalThis.fetch
  globalThis.fetch = async () => new Response(null, { status: 503 })
  try { await assert.rejects(getVaccines(), /HTTP 503/) } finally { globalThis.fetch = original }
})

test('vaccine detail resolves the exact ID from the unfiltered database catalog', async () => {
  const original = globalThis.fetch
  const vaccine = { id: 12, name: '[DEMO] Same name', manufacturer: 'Test', origin: 'Test', price: 250000 }
  globalThis.fetch = async url => {
    const request = new URL(url)
    assert.equal(request.pathname, '/api/v1/catalog/vaccines')
    assert.equal(request.searchParams.get('search'), '')
    return Response.json({ source: 'database', items: [{ ...vaccine, id: 1 }, vaccine] })
  }
  try {
    assert.deepEqual(await getVaccine('12'), vaccine)
    assert.equal(await getVaccine('99'), null)
  } finally { globalThis.fetch = original }
})

test('vaccine detail rejects invalid IDs without fetching', async () => {
  const original = globalThis.fetch
  globalThis.fetch = () => { assert.fail('Invalid IDs must not request a catalog') }
  try {
    for (const id of ['', '0', '-1', '1.2', '12abc', '9007199254740992', '1/2']) assert.equal(await getVaccine(id), null)
  } finally { globalThis.fetch = original }
})

test('vaccine detail does not fall back to demo data or hide API failures', async () => {
  const original = globalThis.fetch
  try {
    globalThis.fetch = async () => Response.json({ source: 'demo', items: [{ id: 1 }] })
    await assert.rejects(getVaccine('1'), /cơ sở dữ liệu/)
    globalThis.fetch = async () => new Response(null, { status: 503 })
    await assert.rejects(getVaccine('1'), /HTTP 503/)
  } finally { globalThis.fetch = original }
})
