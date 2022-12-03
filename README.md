## Discord Rich Presence extension for Visual Studio 2022

This project is an based on [1TheNikita's extension](https://github.com/1thenikita/Visual-Studio). </br>
All visual resources e.g. icons
and languages logos used here are their creators' property

If you have any questions about extension, you can open an issue or write me

## How to start
Just install the extension and open any solution, and your activity will be shown in your discord profile

![Picture 1](https://user-images.githubusercontent.com/56886020/205453588-f7ee0934-a730-4429-adb7-e7afafe7ed25.png)

![Picture 2](https://user-images.githubusercontent.com/56886020/205453606-ca717637-8409-436c-8e50-94c61d029899.png)

## Settings

You can also set up the presence in the settings window </br> 
(Extensions -> Discord Rich Presence -> Settings)

![Picture 3](https://user-images.githubusercontent.com/56886020/205453626-fb3ac338-29e6-4067-9d70-c6401076f4b6.png)

The window contain following preferences: </br>
* **Is Presence Enabled** – globally enables or disables presence
* **Show file name** - enables or disables displaying file name
* **Show solution name** - enables or disables displaying solution name
* **Show timestamp - enables** or disables the timestamp on your user card
* **Reset timestamp on file change** - if disabled, shows total time spent in VS since solution open
* **Large language image** - toggles what will be large image and what will be small - editor logo or language logo
* **Is secret mode active** - activates "secret mode" - 
in this mode you will present that you're in Visual Studio, 
but will not show any specific data about file, solution etc.
* **Load on startup** - needed to load the extension on VS startup 
or on solution opening
* **Use English presence** - if disabled, 
the presence will be shown on your language*
* **Files and directories blacklist** - 
you can load here any text file contains gitignore-like syntax
to filter files and folders you don't want to show them to anyone.
If you open file that is blacklist, 
presence will be disabled while you're in this file.
By default, file dialog filter files ends with ".ignore"
to show that files are like gitignore files.

*-you can help with translating it to your language. 
All localizations are in Translation.\*.resx files,
so you can make its version for your language.
