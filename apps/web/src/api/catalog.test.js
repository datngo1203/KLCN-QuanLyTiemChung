import test from 'node:test'
import assert from 'node:assert/strict'
import { getVaccines, getPackages, getVaccineTypes, getInjectionTypes } from './client.js'

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
