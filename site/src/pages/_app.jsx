import { useRouter } from 'next/router'
import { ThemeProvider } from '@/components/ThemeProvider'
import { Layout } from '@/components/Layout'
import '@/styles/globals.css'

export default function App({ Component, pageProps }) {
  const router = useRouter()

  // Check if this is a documentation page that should use the Layout component
  const isDocumentationPage = router.pathname.startsWith('/docs') ||
                              router.pathname.startsWith('/pricing') ||
                              router.pathname.startsWith('/enterprise') ||
                              router.pathname.startsWith('/community') ||
                              router.pathname.startsWith('/case-studies') ||
                              router.pathname.startsWith('/comparison')

  // For documentation pages, wrap with Layout component
  if (isDocumentationPage) {
    return (
      <ThemeProvider defaultTheme="system">
        <Layout title={pageProps.title} tableOfContents={pageProps.tableOfContents || []}>
          <Component {...pageProps} />
        </Layout>
      </ThemeProvider>
    )
  }

  // For other pages (like landing page), render directly
  return (
    <ThemeProvider defaultTheme="system">
      <Component {...pageProps} />
    </ThemeProvider>
  )
}
