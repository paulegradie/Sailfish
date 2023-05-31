import CodeBlock from '@/components/CodeBlock'

export function Fence({ children, language }) {
  return (
    <CodeBlock code={children} language={language} />
  )
}
