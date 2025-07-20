import { forwardRef, useEffect, useRef, useState } from 'react'
import { cn } from '@/lib/utils'

/**
 * Comprehensive Animation System
 * 
 * Features:
 * - Fade animations (in, out, up, down, left, right)
 * - Scale animations (in, out)
 * - Slide animations (up, down, left, right)
 * - Stagger animations for lists
 * - Intersection Observer for scroll-triggered animations
 * - Consistent with design tokens
 */

// Base animation component
export const Animate = forwardRef(({
  children,
  className,
  animation = 'fade-in',
  duration = 'duration-300',
  delay = 'delay-0',
  easing = 'ease-out',
  trigger = 'immediate', // 'immediate', 'hover', 'scroll'
  threshold = 0.1, // For scroll trigger
  once = true, // Only animate once when scrolling
  ...props
}, ref) => {
  const [isVisible, setIsVisible] = useState(trigger === 'immediate')
  const [hasAnimated, setHasAnimated] = useState(false)
  const elementRef = useRef(null)

  useEffect(() => {
    if (trigger === 'scroll') {
      const observer = new IntersectionObserver(
        ([entry]) => {
          if (entry.isIntersecting && (!once || !hasAnimated)) {
            setIsVisible(true)
            setHasAnimated(true)
          } else if (!once && !entry.isIntersecting) {
            setIsVisible(false)
          }
        },
        { threshold }
      )

      if (elementRef.current) {
        observer.observe(elementRef.current)
      }

      return () => observer.disconnect()
    }
  }, [trigger, threshold, once, hasAnimated])

  const animationClasses = {
    'fade-in': 'animate-fade-in',
    'fade-out': 'animate-fade-out',
    'slide-up': 'animate-slide-up',
    'slide-down': 'animate-slide-down',
    'slide-left': 'animate-slide-left',
    'slide-right': 'animate-slide-right',
    'scale-in': 'animate-scale-in',
    'scale-out': 'animate-scale-out',
    'bounce-subtle': 'animate-bounce-subtle',
  }

  const triggerClasses = {
    hover: 'hover:' + animationClasses[animation],
    immediate: animationClasses[animation],
    scroll: isVisible ? animationClasses[animation] : 'opacity-0',
  }

  return (
    <div
      ref={ref || elementRef}
      className={cn(
        'transition-all',
        duration,
        delay,
        easing,
        triggerClasses[trigger],
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

Animate.displayName = 'Animate'

// Fade animations
export const FadeIn = (props) => <Animate animation="fade-in" {...props} />
export const FadeOut = (props) => <Animate animation="fade-out" {...props} />

// Slide animations
export const SlideUp = (props) => <Animate animation="slide-up" {...props} />
export const SlideDown = (props) => <Animate animation="slide-down" {...props} />
export const SlideLeft = (props) => <Animate animation="slide-left" {...props} />
export const SlideRight = (props) => <Animate animation="slide-right" {...props} />

// Scale animations
export const ScaleIn = (props) => <Animate animation="scale-in" {...props} />
export const ScaleOut = (props) => <Animate animation="scale-out" {...props} />

// Bounce animation
export const BounceSubtle = (props) => <Animate animation="bounce-subtle" {...props} />

// Stagger animation for lists
export const StaggerContainer = ({ 
  children, 
  className,
  staggerDelay = 100, // milliseconds
  ...props 
}) => {
  return (
    <div className={cn('space-y-4', className)} {...props}>
      {Array.isArray(children) 
        ? children.map((child, index) => (
            <Animate
              key={index}
              animation="slide-up"
              delay={`delay-[${index * staggerDelay}ms]`}
              trigger="scroll"
            >
              {child}
            </Animate>
          ))
        : children
      }
    </div>
  )
}

// Hover lift effect
export const HoverLift = forwardRef(({
  children,
  className,
  lift = 'hover:-translate-y-1',
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'transition-transform duration-200 ease-out',
        lift,
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

HoverLift.displayName = 'HoverLift'

// Hover scale effect
export const HoverScale = forwardRef(({
  children,
  className,
  scale = 'hover:scale-105',
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'transition-transform duration-200 ease-out',
        scale,
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

HoverScale.displayName = 'HoverScale'

// Hover glow effect
export const HoverGlow = forwardRef(({
  children,
  className,
  glow = 'hover:shadow-lg',
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'transition-shadow duration-200 ease-out',
        glow,
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

HoverGlow.displayName = 'HoverGlow'

// Loading spinner
export const LoadingSpinner = forwardRef(({
  className,
  size = 'w-6 h-6',
  color = 'border-primary-600',
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'loading-spinner rounded-full border-2 border-gray-300 border-t-transparent',
        size,
        color,
        className
      )}
      {...props}
    />
  )
})

LoadingSpinner.displayName = 'LoadingSpinner'

// Pulse animation for loading states
export const Pulse = forwardRef(({
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn('animate-pulse', className)}
      {...props}
    >
      {children}
    </div>
  )
})

Pulse.displayName = 'Pulse'

// Skeleton loader
export const Skeleton = forwardRef(({
  className,
  width = 'w-full',
  height = 'h-4',
  rounded = 'rounded',
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'animate-pulse bg-gray-200 dark:bg-gray-700',
        width,
        height,
        rounded,
        className
      )}
      {...props}
    />
  )
})

Skeleton.displayName = 'Skeleton'

// Progress bar with animation
export const ProgressBar = forwardRef(({
  value = 0,
  max = 100,
  className,
  showValue = false,
  animated = true,
  color = 'bg-primary-600',
  ...props
}, ref) => {
  const percentage = Math.min(Math.max((value / max) * 100, 0), 100)

  return (
    <div className="w-full">
      <div
        ref={ref}
        className={cn(
          'w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2',
          className
        )}
        {...props}
      >
        <div
          className={cn(
            'h-2 rounded-full transition-all duration-500 ease-out',
            color,
            {
              'animate-pulse': animated && value > 0,
            }
          )}
          style={{ width: `${percentage}%` }}
        />
      </div>
      
      {showValue && (
        <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400 mt-1">
          <span>{value}</span>
          <span>{max}</span>
        </div>
      )}
    </div>
  )
})

ProgressBar.displayName = 'ProgressBar'

// Typewriter effect
export const Typewriter = ({ 
  text, 
  speed = 50, 
  className,
  onComplete,
  ...props 
}) => {
  const [displayText, setDisplayText] = useState('')
  const [currentIndex, setCurrentIndex] = useState(0)

  useEffect(() => {
    if (currentIndex < text.length) {
      const timeout = setTimeout(() => {
        setDisplayText(prev => prev + text[currentIndex])
        setCurrentIndex(prev => prev + 1)
      }, speed)

      return () => clearTimeout(timeout)
    } else if (onComplete) {
      onComplete()
    }
  }, [currentIndex, text, speed, onComplete])

  return (
    <span className={cn('inline-block', className)} {...props}>
      {displayText}
      <span className="animate-pulse">|</span>
    </span>
  )
}

// Parallax effect
export const Parallax = forwardRef(({
  children,
  className,
  speed = 0.5, // 0 = no movement, 1 = normal scroll speed
  ...props
}, ref) => {
  const [offset, setOffset] = useState(0)
  const elementRef = useRef(null)

  useEffect(() => {
    const handleScroll = () => {
      if (elementRef.current) {
        const rect = elementRef.current.getBoundingClientRect()
        const scrolled = window.pageYOffset
        const rate = scrolled * -speed
        setOffset(rate)
      }
    }

    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [speed])

  return (
    <div
      ref={ref || elementRef}
      className={cn('relative', className)}
      style={{ transform: `translateY(${offset}px)` }}
      {...props}
    >
      {children}
    </div>
  )
})

Parallax.displayName = 'Parallax'

// Reveal animation on scroll
export const RevealOnScroll = ({
  children,
  className,
  animation = 'slide-up',
  threshold = 0.1,
  ...props
}) => {
  return (
    <Animate
      animation={animation}
      trigger="scroll"
      threshold={threshold}
      className={className}
      {...props}
    >
      {children}
    </Animate>
  )
}

// Count up animation
export const CountUp = ({
  end,
  start = 0,
  duration = 2000,
  className,
  ...props
}) => {
  const [count, setCount] = useState(start)
  const [isVisible, setIsVisible] = useState(false)
  const elementRef = useRef(null)

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !isVisible) {
          setIsVisible(true)
        }
      },
      { threshold: 0.1 }
    )

    if (elementRef.current) {
      observer.observe(elementRef.current)
    }

    return () => observer.disconnect()
  }, [isVisible])

  useEffect(() => {
    if (isVisible) {
      const increment = (end - start) / (duration / 16) // 60fps
      let current = start

      const timer = setInterval(() => {
        current += increment
        if (current >= end) {
          setCount(end)
          clearInterval(timer)
        } else {
          setCount(Math.floor(current))
        }
      }, 16)

      return () => clearInterval(timer)
    }
  }, [isVisible, start, end, duration])

  return (
    <span ref={elementRef} className={className} {...props}>
      {count}
    </span>
  )
}
