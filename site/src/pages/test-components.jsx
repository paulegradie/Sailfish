import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card'
import { Container, Grid } from '@/components/ui/Grid'
import { ThemeToggle } from '@/components/ui/ThemeToggle'
import { ThemeProvider } from '@/components/ThemeProvider'

export default function TestComponents() {
  return (
    <ThemeProvider>
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-12">
        <Container>
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-4xl font-bold text-gray-900 dark:text-white">
              Component Showcase
            </h1>
            <ThemeToggle />
          </div>

          {/* Button Variants */}
          <Card className="mb-8">
            <CardHeader>
              <CardTitle>Button Components</CardTitle>
              <CardDescription>Various button styles and sizes</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex flex-wrap gap-4">
                  <Button variant="primary">Primary</Button>
                  <Button variant="secondary">Secondary</Button>
                  <Button variant="outline">Outline</Button>
                  <Button variant="ghost">Ghost</Button>
                  <Button variant="success">Success</Button>
                  <Button variant="warning">Warning</Button>
                  <Button variant="error">Error</Button>
                </div>
                <div className="flex flex-wrap gap-4 items-center">
                  <Button size="sm">Small</Button>
                  <Button size="md">Medium</Button>
                  <Button size="lg">Large</Button>
                  <Button size="xl">Extra Large</Button>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Grid System */}
          <Card className="mb-8">
            <CardHeader>
              <CardTitle>Grid System</CardTitle>
              <CardDescription>Responsive grid layouts</CardDescription>
            </CardHeader>
            <CardContent>
              <Grid cols={3} gap={6}>
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-2">Grid Item 1</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      This is a responsive grid item that adapts to screen size.
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-2">Grid Item 2</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      Another grid item showcasing the responsive behavior.
                    </p>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="p-6">
                    <h3 className="font-semibold mb-2">Grid Item 3</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      Third item completing our grid demonstration.
                    </p>
                  </CardContent>
                </Card>
              </Grid>
            </CardContent>
          </Card>

          {/* Color Palette */}
          <Card>
            <CardHeader>
              <CardTitle>Color Palette</CardTitle>
              <CardDescription>Our design system colors</CardDescription>
            </CardHeader>
            <CardContent>
              <Grid cols={6} gap={4}>
                {['primary', 'secondary', 'success', 'warning', 'error', 'gray'].map((color) => (
                  <div key={color} className="text-center">
                    <div className={`h-16 w-full rounded-xl bg-${color}-500 mb-2`} />
                    <p className="text-sm font-medium capitalize">{color}</p>
                  </div>
                ))}
              </Grid>
            </CardContent>
          </Card>
        </Container>
      </div>
    </ThemeProvider>
  )
}