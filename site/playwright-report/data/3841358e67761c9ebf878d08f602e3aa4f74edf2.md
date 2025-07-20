# Page snapshot

```yaml
- alert
- dialog "Server Error":
  - navigation:
    - button [disabled]:
      - img
    - button [disabled]:
      - img
    - text: 1 of 1 unhandled error
  - heading "Server Error" [level=1]
  - paragraph: "TypeError: Cannot read properties of undefined (reading '0')"
  - text: This error happened while generating the page. Any console logs will be displayed in the terminal window.
  - heading "Source" [level=5]
  - link "src\\components\\Layout.jsx (125:71) @ useTableOfContents":
    - text: src\components\Layout.jsx (125:71) @ useTableOfContents
    - img
  - text: "123 | 124 | function useTableOfContents(tableOfContents) { > 125 | let [currentSection, setCurrentSection] = useState(tableOfContents[0]?.id) | ^ 126 | 127 | let getHeadings = useCallback((tableOfContents) => { 128 | return tableOfContents"
  - heading "Call Stack" [level=5]
  - heading "Layout" [level=6]
  - link "src\\components\\Layout.jsx (177:25)":
    - text: src\components\Layout.jsx (177:25)
    - img
  - button "Show collapsed frames"
```