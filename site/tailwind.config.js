/** @type {import('tailwindcss').Config} */
const {
  colors,
  typography,
  spacing,
  screens,
  animations,
  shadows,
  borderRadius,
  zIndex
} = require('./src/styles/design-tokens')

module.exports = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,md}',
    './src/components/**/*.{js,ts,jsx,tsx}',
    './src/layouts/**/*.{js,ts,jsx,tsx}',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      // Design token-based colors
      colors: {
        // Brand colors
        primary: colors.brand.primary,
        secondary: colors.brand.secondary,

        // Semantic colors
        success: colors.semantic.success,
        warning: colors.semantic.warning,
        error: colors.semantic.error,
        info: colors.semantic.info,

        // Neutral colors
        gray: colors.neutral,
        neutral: colors.neutral,
      },
      // Typography system from design tokens
      fontFamily: typography.fontFamily,
      fontSize: typography.fontSize,
      fontWeight: typography.fontWeight,

      // Spacing system from design tokens
      spacing: spacing,

      // Responsive breakpoints
      screens: screens,
      // Animation system from design tokens
      animation: {
        'fade-in': 'fadeIn 0.5s ease-in-out',
        'fade-out': 'fadeOut 0.5s ease-in-out',
        'slide-up': 'slideUp 0.3s ease-out',
        'slide-down': 'slideDown 0.3s ease-out',
        'slide-left': 'slideLeft 0.3s ease-out',
        'slide-right': 'slideRight 0.3s ease-out',
        'scale-in': 'scaleIn 0.2s ease-out',
        'scale-out': 'scaleOut 0.2s ease-out',
        'bounce-subtle': 'bounceSubtle 0.6s ease-in-out',
        'pulse': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'spin': 'spin 1s linear infinite',
      },
      keyframes: animations.keyframes,

      // Transition duration and timing
      transitionDuration: animations.duration,
      transitionTimingFunction: animations.timingFunction,

      // Box shadows from design tokens
      boxShadow: shadows,

      // Border radius from design tokens
      borderRadius: borderRadius,

      // Z-index system
      zIndex: zIndex,
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
    require('@tailwindcss/aspect-ratio'),

    // Custom plugin for additional utilities
    function({ addUtilities, addComponents, theme }) {
      // Custom utilities for better developer experience
      addUtilities({
        // Smooth scrolling
        '.scroll-smooth': {
          'scroll-behavior': 'smooth',
        },

        // Better focus styles
        '.focus-ring': {
          '&:focus': {
            outline: '2px solid transparent',
            'outline-offset': '2px',
            'box-shadow': `0 0 0 2px ${theme('colors.primary.500')}`,
          },
        },

        // Glass morphism effect
        '.glass': {
          'backdrop-filter': 'blur(16px) saturate(180%)',
          'background-color': 'rgba(255, 255, 255, 0.75)',
          'border': '1px solid rgba(209, 213, 219, 0.3)',
        },

        '.glass-dark': {
          'backdrop-filter': 'blur(16px) saturate(180%)',
          'background-color': 'rgba(17, 25, 40, 0.75)',
          'border': '1px solid rgba(255, 255, 255, 0.125)',
        },

        // Gradient text
        '.gradient-text': {
          'background': `linear-gradient(135deg, ${theme('colors.primary.600')}, ${theme('colors.secondary.500')})`,
          '-webkit-background-clip': 'text',
          'background-clip': 'text',
          '-webkit-text-fill-color': 'transparent',
        },
      })

      // Custom components
      addComponents({
        // Button variants
        '.btn': {
          'padding': `${theme('spacing.2')} ${theme('spacing.4')}`,
          'border-radius': theme('borderRadius.lg'),
          'font-weight': theme('fontWeight.medium'),
          'transition': 'all 0.2s ease-in-out',
          'cursor': 'pointer',
          'display': 'inline-flex',
          'align-items': 'center',
          'justify-content': 'center',
          'gap': theme('spacing.2'),

          '&:disabled': {
            'opacity': '0.5',
            'cursor': 'not-allowed',
          },
        },

        '.btn-primary': {
          'background-color': theme('colors.primary.600'),
          'color': theme('colors.white'),

          '&:hover:not(:disabled)': {
            'background-color': theme('colors.primary.700'),
            'transform': 'translateY(-1px)',
            'box-shadow': theme('boxShadow.lg'),
          },

          '&:active': {
            'transform': 'translateY(0)',
          },
        },

        '.btn-secondary': {
          'background-color': theme('colors.gray.100'),
          'color': theme('colors.gray.900'),
          'border': `1px solid ${theme('colors.gray.300')}`,

          '&:hover:not(:disabled)': {
            'background-color': theme('colors.gray.200'),
            'border-color': theme('colors.gray.400'),
          },
        },

        // Card component
        '.card': {
          'background-color': theme('colors.white'),
          'border-radius': theme('borderRadius.xl'),
          'box-shadow': theme('boxShadow.soft'),
          'padding': theme('spacing.6'),
          'transition': 'all 0.2s ease-in-out',

          '&:hover': {
            'box-shadow': theme('boxShadow.medium'),
            'transform': 'translateY(-2px)',
          },
        },

        '.card-dark': {
          'background-color': theme('colors.gray.800'),
          'color': theme('colors.white'),
        },
      })
    },
  ],
}
