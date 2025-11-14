import { nodes as defaultNodes } from '@markdoc/markdoc'

import { Fence } from '@/components/Fence'
import { Link } from '@/components/MarkdocLink'

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
  },
}

export default nodes
