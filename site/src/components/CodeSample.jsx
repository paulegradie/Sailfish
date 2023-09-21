import { Fragment, useState } from 'react';
import clsx from 'clsx';
import CodeBlock from '@/components/CodeBlock';
import { TrafficLightsIcon } from './Hero';

const testCode = `[Sailfish]
public class AMostBasicTest
{
    [SailfishVariable(true, 10, 100, 1000)]
    public int N { get; set; }

    [SailfishMethod]
    public void Method() => Thread.Sleep(N);
}
`;
const rego = `public class RegoProvider : IProvideARegistrationCallback
{
    public async Task RegisterAsync(
        ContainerBuilder builder, CancellationToken ct)
    {
        await Task.CompletedTask;
        builder.RegisterType<WebApplicationFactory<DemoApp>>();
    }
}
`;

const tabs = [
    { name: 'BasicTest.cs', code: testCode },
    { name: 'RegistrationProvider.cs', code: rego }
];


export const CodeSample = () => {
    const [activeTab, setActiveTab] = useState(0);
    const changeTab = (tabNumber) => setActiveTab(tabNumber);
    return (
        <>
            <div className="absolute inset-0 rounded-2xl bg-gradient-to-tr from-sky-300 via-sky-300/70 to-blue-300 opacity-10 blur-lg" />
            <div className="absolute inset-0 rounded-2xl bg-gradient-to-tr from-sky-300 via-sky-300/70 to-blue-300 opacity-10" />
            <div className="relative rounded-2xl bg-[#0A101F]/80 ring-1 ring-white/10 backdrop-blur">
                <div className="absolute -top-px left-20 right-11 h-px bg-gradient-to-r from-sky-300/0 via-sky-300/70 to-sky-300/0" />
                <div className="absolute -bottom-px left-11 right-20 h-px bg-gradient-to-r from-blue-400/0 via-blue-400 to-blue-400/0" />
                <div className="pl-4 pt-4">
                    <TrafficLightsIcon className="h-2.5 w-auto stroke-slate-500/30" />
                    <div className="mt-4 flex space-x-2 text-xs">
                        {tabs.map((tab, i) => (
                            <div
                                onClick={() => changeTab(i)}
                                key={tab.name + i}
                                className={clsx(
                                    'flex h-6 rounded-full cursor-pointer',
                                    activeTab == i
                                        ? 'bg-gradient-to-r from-sky-400/30 via-sky-400 to-sky-400/30 p-px font-medium text-sky-300'
                                        : 'text-slate-500'
                                )}
                            >
                                <div
                                    className={clsx(
                                        'flex items-center rounded-full px-2.5',
                                        i === activeTab && 'bg-slate-800'
                                    )}
                                >
                                    {tab.name}
                                </div>
                            </div>
                        ))}
                    </div>
                    <div className="mt-6 flex items-start px-1 text-sm">
                        <div
                            aria-hidden="true"
                            className="select-none border-r border-slate-300/5 pr-4 font-mono text-slate-600"
                        >
                            {Array.from({
                                length: tabs[activeTab].code.split('\n').length,
                            }).map((_, index) => (
                                <Fragment key={index}>
                                    {(index + 1).toString().padStart(2, '0')}
                                    <br />
                                </Fragment>
                            ))}
                        </div>
                        <CodeBlock language="csharp" code={tabs[activeTab].code} />
                    </div>
                </div>
            </div>
        </>
    );
};
