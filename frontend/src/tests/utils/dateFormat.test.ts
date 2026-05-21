import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { formatDate, formatLastActivity } from '@/utils/dateFormat'

// Pin "now" to a fixed point so diffDays calculations are deterministic.
const FIXED_NOW = new Date('2024-06-15T12:00:00Z').getTime()

describe('formatLastActivity', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(FIXED_NOW)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('returns "—" for null input', () => {
    expect(formatLastActivity(null)).toBe('—')
  })

  it('returns "Today" when the timestamp is the current moment', () => {
    const iso = new Date(FIXED_NOW).toISOString()
    expect(formatLastActivity(iso)).toBe('Today')
  })

  it('returns "Today" when the timestamp is a few hours ago', () => {
    const iso = new Date(FIXED_NOW - 3 * 3_600_000).toISOString()
    expect(formatLastActivity(iso)).toBe('Today')
  })

  it('returns "Yesterday" for exactly 24 hours ago', () => {
    const iso = new Date(FIXED_NOW - 86_400_000).toISOString()
    expect(formatLastActivity(iso)).toBe('Yesterday')
  })

  it('returns "3d ago" for 3 days ago', () => {
    const iso = new Date(FIXED_NOW - 3 * 86_400_000).toISOString()
    expect(formatLastActivity(iso)).toBe('3d ago')
  })
})

describe('formatDate', () => {
  it('returns a non-empty string for a valid ISO date', () => {
    const result = formatDate('2024-01-15T10:00:00Z')
    expect(typeof result).toBe('string')
    expect(result.length).toBeGreaterThan(0)
  })
})
