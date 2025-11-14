import NextLink from 'next/link'
import { useRouter } from 'next/router'

export function Link({ href, children, ...props }) {
  const router = useRouter()
  
  // Check if this is an internal link (starts with /)
  const isInternalLink = href && href.startsWith('/')
  
  // For internal links, use Next.js Link component which respects basePath
  if (isInternalLink) {
    return (
      <NextLink href={href} {...props}>
        {children}
      </NextLink>
    )
  }
  
  // For external links, use regular anchor tag
  return (
    <a href={href} {...props}>
      {children}
    </a>
  )
}

