import { nodes as defaultNodes } from '@markdoc/markdoc'
import Link from 'next/link'

import { Fence } from '@/components/Fence'

const nodes = {
  document: {
    render: undefined,
  },
  th: {
    ...defaultNodes.th,
    attributes: {
      ...defaultNodes.th.attributes,
      scope: {
        type: String,
        default: 'col',
      },
    },
  },
  fence: {
    render: Fence,
    attributes: {
      language: {
        type: String,
      },
    },
  },
  link: {
    render: Link,
    attributes: {
      href: {
        type: String,
      },
    },
  },
}

export default nodes
