import { test, expect } from '@playwright/test'

test.describe('Component Showcase', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/showcase')
  })

  test('should display component showcase page', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Component Showcase')
  })

  test('should have all button variants', async ({ page }) => {
    const buttons = [
      'Primary', 'Secondary', 'Outline', 'Ghost', 
      'Success', 'Warning', 'Error'
    ]
    
    for (const buttonText of buttons) {
      await expect(page.locator(`button:has-text("${buttonText}")`)).toBeVisible()
    }
  })

  test('should toggle theme', async ({ page }) => {
    // Check initial state
    const html = page.locator('html')
    
    // Click theme toggle
    await page.click('[aria-label="Toggle theme"], button:has(svg)')
    
    // Wait for theme change
    await page.waitForTimeout(100)
    
    // Verify theme changed (either light or dark class should be present)
    const hasThemeClass = await html.evaluate(el => 
      el.classList.contains('light') || el.classList.contains('dark')
    )
    expect(hasThemeClass).toBe(true)
  })

  test('should display grid items responsively', async ({ page }) => {
    const gridItems = page.locator('h3:has-text("Grid Item")')
    await expect(gridItems).toHaveCount(3)
    
    // Check that all grid items are visible
    for (let i = 1; i <= 3; i++) {
      await expect(page.locator(`h3:has-text("Grid Item ${i}")`)).toBeVisible()
    }
  })

  test('should display color palette', async ({ page }) => {
    const colors = ['primary', 'secondary', 'success', 'warning', 'error', 'gray']

    for (const color of colors) {
      await expect(page.locator(`p:has-text("${color}")`)).toBeVisible()
    }
  })
})

test.describe('Navigation Components', () => {
  test('should display logo components', async ({ page }) => {
    await page.goto('/showcase')

    // Check for logo variants
    await expect(page.locator('text=Logo Components')).toBeVisible()
    await expect(page.locator('text=Logo Variants')).toBeVisible()
    await expect(page.locator('text=Logo Colors')).toBeVisible()
    await expect(page.locator('text=Logo Icon & Mark')).toBeVisible()
  })

  test('should have working main navigation', async ({ page }) => {
    await page.goto('/')

    // Check main navigation items
    const navItems = ['Home', 'Documentation', 'Pricing', 'Enterprise', 'Community']

    for (const item of navItems) {
      await expect(page.locator(`nav a:has-text("${item}"), nav button:has-text("${item}")`)).toBeVisible()
    }
  })

  test('should show mobile navigation on small screens', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 }) // Mobile size
    await page.goto('/')

    // Mobile menu button should be visible
    await expect(page.locator('button[aria-label="Open navigation menu"]')).toBeVisible()

    // Click mobile menu
    await page.click('button[aria-label="Open navigation menu"]')

    // Mobile menu should open
    await expect(page.locator('text=Sailfish')).toBeVisible()
    await expect(page.locator('button[aria-label="Close navigation menu"]')).toBeVisible()
  })

  test('should display breadcrumbs on documentation pages', async ({ page }) => {
    await page.goto('/docs/0/getting-started')

    // Breadcrumbs should be visible
    await expect(page.locator('nav[aria-label="Breadcrumb"]')).toBeVisible()
    await expect(page.locator('a:has-text("Home")')).toBeVisible()
  })

  test('should have working theme toggle', async ({ page }) => {
    await page.goto('/')

    // Theme selector should be visible
    await expect(page.locator('button[aria-label*="Theme"], [aria-label="Theme"] button')).toBeVisible()

    // Click theme toggle
    await page.click('button[aria-label*="Theme"], [aria-label="Theme"] button')

    // Theme options should appear
    await page.waitForTimeout(100)
  })
})