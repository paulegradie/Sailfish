import { forwardRef } from 'react'
import { cn } from '@/lib/utils'

const Container = forwardRef(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={cn(
      'mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8',
      className
    )}
    {...props}
  />
))
Container.displayName = 'Container'

const Grid = forwardRef(({ className, cols = 1, gap = 6, ...props }, ref) => {
  const gridCols = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 md:grid-cols-2',
    3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4',
    6: 'grid-cols-2 md:grid-cols-3 lg:grid-cols-6',
    12: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 2xl:grid-cols-12',
  }

  const gridGap = {
    2: 'gap-2',
    4: 'gap-4',
    6: 'gap-6',
    8: 'gap-8',
    12: 'gap-12',
  }

  return (
    <div
      ref={ref}
      className={cn(
        'grid',
        gridCols[cols] || 'grid-cols-1',
        gridGap[gap] || 'gap-6',
        className
      )}
      {...props}
    />
  )
})
Grid.displayName = 'Grid'

export { Container, Grid }