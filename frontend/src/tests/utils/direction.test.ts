import { describe, it, expect, beforeEach } from 'vitest'
import { getDirection, isRtl, applyDocumentDirection } from '@/utils/direction'

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

describe('applyDocumentDirection', () => {
  beforeEach(() => {
    // Reset to neutral state before each test
    document.documentElement.removeAttribute('dir')
    document.documentElement.removeAttribute('lang')
  })

  it('sets dir="ltr" and lang="en" for English', () => {
    applyDocumentDirection('en')
    expect(document.documentElement.getAttribute('dir')).toBe('ltr')
    expect(document.documentElement.getAttribute('lang')).toBe('en')
  })

  it('sets dir="rtl" and lang="ar" for Arabic', () => {
    applyDocumentDirection('ar')
    expect(document.documentElement.getAttribute('dir')).toBe('rtl')
    expect(document.documentElement.getAttribute('lang')).toBe('ar')
  })

  it('sets dir="rtl" and lang="fa" for Persian', () => {
    applyDocumentDirection('fa')
    expect(document.documentElement.getAttribute('dir')).toBe('rtl')
    expect(document.documentElement.getAttribute('lang')).toBe('fa')
  })

  it('sets dir="ltr" for an unknown locale', () => {
    applyDocumentDirection('xx')
    expect(document.documentElement.getAttribute('dir')).toBe('ltr')
    expect(document.documentElement.getAttribute('lang')).toBe('xx')
  })
})
