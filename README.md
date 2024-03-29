The web bloat score is calculated as `WebBS = TotalPageSize / PageImageSize`

## How to run the project
WebBS-Calculator uses the [SlimmerJS](https://github.com/laurentj/slimerjs) scriptable browser that requires Firefox version 48 or older to be installed on your computer.
You can download it from Mozzila's [archive](https://ftp.mozilla.org/pub/firefox/releases/48.0/). 

If you don’t choose the default install location (C:\Program Files\Mozilla Firefox 48\firefox.exe), you need to let SlimmerJS know by modifying the [WebBloatScore/Slimerjs/slimmer.bat](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Slimerjs/slimerjs.bat)
file on lines [202](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Slimerjs/slimerjs.bat#L202) and [203](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Slimerjs/slimerjs.bat#L203) with the path you chose during the installation.

After that you can simply run the WebBloatScore ASP.NET MVC application.
 
## Calculation
In the project the calculation is happening in the [SlimmerExecutor](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Models/SlimerExecutor.cs) class:
1. First SlimmerJS is [executed](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Models/SlimerExecutor.cs#L70). It creates and stores a screenshot of the web page and returns the total size of the page.
2. After that [PngQuant](https://github.com/kornelski/pngquant) library is used to [optimize](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Models/SlimerExecutor.cs#L125) the screenshot of the page.
3. In the end the result with all values necessary to calculate WebBS is [returned](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Models/SlimerExecutor.cs#L110).

## Performance
The tasks necessary to calculate the score are time-consuming and depending on the hardware can take several minutes to complete.
Therefore the [timeout](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Controllers/HomeController.cs#L93) is used
to limit the execution time. On top of that [caching](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Controllers/HomeController.cs#L85) of the most used [websites](https://github.com/ZSvedic/WebBS-Calculator/blob/master/WebBloatScore/Utilities.cs#L16) is used to save resources.

## Issue with Let's Encrypt SSL certificates
In the [Issue 3](https://github.com/ZSvedic/WebBS-Calculator/issues/3) it was noted that serveral websites stopped working. The issue is happening because of the combination of SlimmerJS and Firefox version 48 which doesn't trust certificates of given websites even though they are valid. We noticed this is happening for website which have the certificate from Let's Encrypt.

These tools are obsolote and became hard to maintain and it would be necessary to replace them. We currently don't plan to expand the tool so this issue remains unresolved. However, we would be happy to accept the help with this issue from the community.
