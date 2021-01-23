# Standard Quelle Driver Interpreter

A tandem project in github provides a dependent library and a standard Quelle driver.
Using the StandardClarityDriver* projects (with the ClarityHMI library), there are less than 500 
lines of code to extend and/or modify to customize your own Quelle compliant driver/interpreter.
The value proposition here is that parsing is tedious. And starting your search CLI with a concise syntax
with an easy to digest parsing library could easily save your team a person-year in design-time and coding.
Quelle* source code is licensed with an MIT license.
<br/></br>
The design incentive for ClarityHMI and the standard driver/interpreter was initially to support the broader Digital-AV effort: That project [Digital-AV] provides a command-line interface for searching and publishing the KJV bible. Every attempt has been made to keep the Quelle syntax agnostic about the search domain, yet the Quelle user documentation itself [coming soon to github is heavily biased in its syntax examples. Still, the search domain of the StandardClarityDriver remains unbiased.

LAYERS:
StandardClarityDriverInterpreter --> StandardClarityDriver --> ClarityHMI --> dotnet-core --> any OS