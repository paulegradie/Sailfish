/**
 * Design Tokens System
 * Centralized design system tokens for consistent styling across the application
 */

// Color System
export const colors = {
  // Brand Colors
  brand: {
    primary: {
      50: '#eff6ff',
      100: '#dbeafe',
      200: '#bfdbfe',
      300: '#93c5fd',
      400: '#60a5fa',
      500: '#3b82f6', // Main brand color
      600: '#2563eb',
      700: '#1d4ed8',
      800: '#1e40af',
      900: '#1e3a8a',
      950: '#172554',
    },
    secondary: {
      50: '#f0f9ff',
      100: '#e0f2fe',
      200: '#bae6fd',
      300: '#7dd3fc',
      400: '#38bdf8',
      500: '#0ea5e9',
      600: '#0284c7',
      700: '#0369a1',
      800: '#075985',
      900: '#0c4a6e',
      950: '#082f49',
    },
  },
  
  // Semantic Colors
  semantic: {
    success: {
      50: '#f0fdf4',
      100: '#dcfce7',
      200: '#bbf7d0',
      300: '#86efac',
      400: '#4ade80',
      500: '#22c55e',
      600: '#16a34a',
      700: '#15803d',
      800: '#166534',
      900: '#14532d',
      950: '#052e16',
    },
    warning: {
      50: '#fffbeb',
      100: '#fef3c7',
      200: '#fde68a',
      300: '#fcd34d',
      400: '#fbbf24',
      500: '#f59e0b',
      600: '#d97706',
      700: '#b45309',
      800: '#92400e',
      900: '#78350f',
      950: '#451a03',
    },
    error: {
      50: '#fef2f2',
      100: '#fee2e2',
      200: '#fecaca',
      300: '#fca5a5',
      400: '#f87171',
      500: '#ef4444',
      600: '#dc2626',
      700: '#b91c1c',
      800: '#991b1b',
      900: '#7f1d1d',
      950: '#450a0a',
    },
    info: {
      50: '#f0f9ff',
      100: '#e0f2fe',
      200: '#bae6fd',
      300: '#7dd3fc',
      400: '#38bdf8',
      500: '#0ea5e9',
      600: '#0284c7',
      700: '#0369a1',
      800: '#075985',
      900: '#0c4a6e',
      950: '#082f49',
    },
  },
  
  // Neutral Colors
  neutral: {
    50: '#f8fafc',
    100: '#f1f5f9',
    200: '#e2e8f0',
    300: '#cbd5e1',
    400: '#94a3b8',
    500: '#64748b',
    600: '#475569',
    700: '#334155',
    800: '#1e293b',
    900: '#0f172a',
    950: '#020617',
  },
}

// Typography System
export const typography = {
  fontFamily: {
    sans: ['Inter', 'system-ui', 'sans-serif'],
    mono: ['JetBrains Mono', 'Menlo', 'Monaco', 'monospace'],
    display: ['Cal Sans', 'Inter', 'system-ui', 'sans-serif'],
  },
  
  fontSize: {
    // Text sizes
    'xs': ['0.75rem', { lineHeight: '1rem', letterSpacing: '0.025em' }],
    'sm': ['0.875rem', { lineHeight: '1.25rem', letterSpacing: '0.025em' }],
    'base': ['1rem', { lineHeight: '1.5rem', letterSpacing: '0' }],
    'lg': ['1.125rem', { lineHeight: '1.75rem', letterSpacing: '0' }],
    'xl': ['1.25rem', { lineHeight: '1.75rem', letterSpacing: '0' }],
    
    // Display sizes
    '2xl': ['1.5rem', { lineHeight: '2rem', letterSpacing: '-0.025em' }],
    '3xl': ['1.875rem', { lineHeight: '2.25rem', letterSpacing: '-0.025em' }],
    '4xl': ['2.25rem', { lineHeight: '2.5rem', letterSpacing: '-0.025em' }],
    '5xl': ['3rem', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
    '6xl': ['3.75rem', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
    '7xl': ['4.5rem', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
    '8xl': ['6rem', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
    '9xl': ['8rem', { lineHeight: '1.1', letterSpacing: '-0.025em' }],
  },
  
  fontWeight: {
    thin: '100',
    extralight: '200',
    light: '300',
    normal: '400',
    medium: '500',
    semibold: '600',
    bold: '700',
    extrabold: '800',
    black: '900',
  },
}

// Spacing System (8px grid)
export const spacing = {
  px: '1px',
  0: '0',
  0.5: '0.125rem', // 2px
  1: '0.25rem',    // 4px
  1.5: '0.375rem', // 6px
  2: '0.5rem',     // 8px
  2.5: '0.625rem', // 10px
  3: '0.75rem',    // 12px
  3.5: '0.875rem', // 14px
  4: '1rem',       // 16px
  5: '1.25rem',    // 20px
  6: '1.5rem',     // 24px
  7: '1.75rem',    // 28px
  8: '2rem',       // 32px
  9: '2.25rem',    // 36px
  10: '2.5rem',    // 40px
  11: '2.75rem',   // 44px
  12: '3rem',      // 48px
  14: '3.5rem',    // 56px
  16: '4rem',      // 64px
  18: '4.5rem',    // 72px
  20: '5rem',      // 80px
  24: '6rem',      // 96px
  28: '7rem',      // 112px
  32: '8rem',      // 128px
  36: '9rem',      // 144px
  40: '10rem',     // 160px
  44: '11rem',     // 176px
  48: '12rem',     // 192px
  52: '13rem',     // 208px
  56: '14rem',     // 224px
  60: '15rem',     // 240px
  64: '16rem',     // 256px
  72: '18rem',     // 288px
  80: '20rem',     // 320px
  88: '22rem',     // 352px
  96: '24rem',     // 384px
  128: '32rem',    // 512px
  144: '36rem',    // 576px
}

// Breakpoints
export const screens = {
  'xs': '475px',
  'sm': '640px',
  'md': '768px',
  'lg': '1024px',
  'xl': '1280px',
  '2xl': '1536px',
}

// Animation System
export const animations = {
  // Duration
  duration: {
    75: '75ms',
    100: '100ms',
    150: '150ms',
    200: '200ms',
    300: '300ms',
    500: '500ms',
    700: '700ms',
    1000: '1000ms',
  },
  
  // Timing functions
  timingFunction: {
    'ease-in': 'cubic-bezier(0.4, 0, 1, 1)',
    'ease-out': 'cubic-bezier(0, 0, 0.2, 1)',
    'ease-in-out': 'cubic-bezier(0.4, 0, 0.2, 1)',
    'bounce': 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
    'smooth': 'cubic-bezier(0.25, 0.46, 0.45, 0.94)',
  },
  
  // Custom animations
  keyframes: {
    fadeIn: {
      '0%': { opacity: '0' },
      '100%': { opacity: '1' },
    },
    fadeOut: {
      '0%': { opacity: '1' },
      '100%': { opacity: '0' },
    },
    slideUp: {
      '0%': { transform: 'translateY(10px)', opacity: '0' },
      '100%': { transform: 'translateY(0)', opacity: '1' },
    },
    slideDown: {
      '0%': { transform: 'translateY(-10px)', opacity: '0' },
      '100%': { transform: 'translateY(0)', opacity: '1' },
    },
    slideLeft: {
      '0%': { transform: 'translateX(10px)', opacity: '0' },
      '100%': { transform: 'translateX(0)', opacity: '1' },
    },
    slideRight: {
      '0%': { transform: 'translateX(-10px)', opacity: '0' },
      '100%': { transform: 'translateX(0)', opacity: '1' },
    },
    scaleIn: {
      '0%': { transform: 'scale(0.95)', opacity: '0' },
      '100%': { transform: 'scale(1)', opacity: '1' },
    },
    scaleOut: {
      '0%': { transform: 'scale(1)', opacity: '1' },
      '100%': { transform: 'scale(0.95)', opacity: '0' },
    },
    bounceSubtle: {
      '0%, 100%': { transform: 'translateY(0)' },
      '50%': { transform: 'translateY(-5px)' },
    },
    pulse: {
      '0%, 100%': { opacity: '1' },
      '50%': { opacity: '0.5' },
    },
    spin: {
      '0%': { transform: 'rotate(0deg)' },
      '100%': { transform: 'rotate(360deg)' },
    },
  },
}

// Shadow System
export const shadows = {
  'xs': '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
  'sm': '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
  'md': '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
  'lg': '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
  'xl': '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
  '2xl': '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
  'inner': 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.06)',
  'soft': '0 2px 15px -3px rgba(0, 0, 0, 0.07), 0 10px 20px -2px rgba(0, 0, 0, 0.04)',
  'medium': '0 4px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
  'large': '0 10px 40px -10px rgba(0, 0, 0, 0.15), 0 4px 25px -5px rgba(0, 0, 0, 0.1)',
  'inner-soft': 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.06)',
}

// Border Radius System
export const borderRadius = {
  'none': '0',
  'sm': '0.125rem',
  'md': '0.375rem',
  'lg': '0.5rem',
  'xl': '0.75rem',
  '2xl': '1rem',
  '3xl': '1.5rem',
  'full': '9999px',
}

// Z-Index System
export const zIndex = {
  0: '0',
  10: '10',
  20: '20',
  30: '30',
  40: '40',
  50: '50',
  'dropdown': '1000',
  'sticky': '1020',
  'fixed': '1030',
  'modal-backdrop': '1040',
  'modal': '1050',
  'popover': '1060',
  'tooltip': '1070',
  'toast': '1080',
}
