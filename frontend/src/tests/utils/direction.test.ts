import { describe, it, expect } from 'vitest'
import { getDirection, isRtl } from '@/utils/direction'

describe('direction utils', () => {
  it('returns ltr for English', () => {
    expect(getDirection('en')).toBe('ltr')
  })

  it('returns rtl for Arabic', () => {
    expect(getDirection('ar')).toBe('rtl')
  })

  it('returns rtl for Persian', () => {
    expect(getDirection('fa')).toBe('rtl')
  })

  it('returns ltr for unknown locale', () => {
    expect(getDirection('xx')).toBe('ltr')
  })

  it('isRtl returns false for English', () => {
    expect(isRtl('en')).toBe(false)
  })

  it('isRtl returns true for Hebrew', () => {
    expect(isRtl('he')).toBe(true)
  })
})
