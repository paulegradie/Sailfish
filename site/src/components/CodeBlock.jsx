import clsx from 'clsx'
import Highlight, { Prism, defaultProps } from "prism-react-renderer";


(typeof global !== "undefined" ? global : window).Prism = Prism
await import("prismjs/components/prism-applescript")
require("prismjs/components/prism-csharp");


export default function CodeBlock({ language, code }) {

    return (
        <Highlight
            {...defaultProps}
            code={code}
            language={language}
            theme={undefined}
        >
            {({
                className,
                style,
                tokens,
                getLineProps,
                getTokenProps,
            }) => (
                <pre
                    className={clsx(
                        className,
                        'flex overflow-x-auto'
                    )}
                    style={style}
                >
                    <code className="px-2 pb-0">
                        {tokens.slice(0, tokens.length - 1).map((line, lineIndex) => (
                            <div key={lineIndex} {...getLineProps({ line })}>
                                {line.map((token, tokenIndex) => (
                                    <span
                                        key={tokenIndex}
                                        {...getTokenProps({ token })}
                                    />
                                ))}
                            </div>
                        ))}
                    </code>
                </pre>
            )}
        </Highlight>
    )
}