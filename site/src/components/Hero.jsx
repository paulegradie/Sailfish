

import { Button } from '@/components/Button'




import { CodeSample } from './CodeSample'


export function TrafficLightsIcon(props) {
    return (
        <svg aria-hidden="true" viewBox="0 0 42 10" fill="none" {...props}>
            <circle cx="5" cy="5" r="4.5" />
            <circle cx="21" cy="5" r="4.5" />
            <circle cx="37" cy="5" r="4.5" />
        </svg>
    )
}

export function Hero() {
    return (
        <div className="overflow-hidden bg-slate-900 dark:-mb-32 dark:mt-[-4.5rem] dark:pb-32 dark:pt-[4.5rem] dark:lg:mt-[-4.75rem] dark:lg:pt-[4.75rem]">
            <div className="relative py-16 sm:px-2 lg:py-20 lg:px-0">
                <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
                    <div className="absolute left-1/2 top-[-8rem] h-72 w-72 -translate-x-1/2 rounded-full bg-gradient-to-tr from-primary-400/30 via-accent-400/20 to-transparent blur-3xl" />
                    <div className="absolute right-[-6rem] bottom-[-6rem] h-96 w-96 rounded-full bg-gradient-to-tr from-accent-400/20 via-primary-400/20 to-transparent blur-3xl" />
                    <div className="absolute left-0 top-1/2 h-px w-full bg-gradient-to-r from-transparent via-slate-200/50 to-transparent dark:via-slate-700/40" />
                </div>
                <div className="mx-auto grid max-w-2xl grid-cols-1 items-center gap-y-16 gap-x-8 px-4 lg:max-w-8xl lg:grid-cols-2 lg:px-8 xl:gap-x-16 xl:px-12">
                    <div className="relative z-10 md:text-center lg:text-left">

                        <div className="relative">
                            <p className="inline bg-gradient-to-r from-indigo-200 via-primary-400 to-indigo-200 bg-clip-text font-display text-5xl tracking-tight text-transparent">
                                Sailfish
                            </p>
                            <div className="mt-8 flex gap-4 md:justify-center lg:justify-start">
                                <Button href="/docs/0/getting-started">Get started</Button>
                                <Button href="https://github.com/paulegradie/Sailfish" variant="secondary">
                                    View on GitHub
                                </Button>
                            </div>
                        </div>
                    </div>
                    <div className="relative lg:static xl:pl-10">

                        <div className="relative">


                            <CodeSample />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    )
}




