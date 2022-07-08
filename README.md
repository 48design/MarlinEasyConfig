# MarlinEasyConfig [![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/donate/?hosted_button_id=VT4P4AT8FTYDL)
A configuration tool for [Marlin](https://marlinfw.org/) to make editing Marlin configurations an easy task.

## How to
**Note**: This software is still in development. It has been tested in personal use and bugs are sure to be found!

MarlinEasyConfig can be used to open and edit Marlin's Configuration.h and Configuration_adv.h files.
- Simply pick a Marlin folder (`Menu > Open Marlin Folder`) and the configuration files will get parsed. All variables are listed in a data table with their values and comments.
- You can search for a variable name using the top right text field. The table gets filtered.
- Edit the values and save the configuration (`Menu > Save Config`).
- Compare the configuration loaded with a different Marlin configuration (`Menu > Compare Marlin Configuration`). If needed you can transfer all values from the compared version (`Menu > Transfer Compared Values`), e.g. to switch to a new Marlin version.
- Restore your configuration to get back the last state (`Menu > Restore Configuration`)

## Features
- **read** and **edit** Marlin configuration files
- **compare** Marlin configurations
- **transfer settings** from one configuration to another
- **backup** and **restore** of the configuration (Configuration.bak and Configuration_adv.bak files created on save)

## ToDos
- parse commented defines (`//#define`) and show a checkbox for each row to switch between those states
- ... 

## Ideas
- check Marlin versions and provide a way to transfer values between renamed variables based on version number
- ... 

## Donation
If this project helps you to reduce time changing Marlin versions or simply its configuration, you can sponsor us a cup of :coffee: - or two! :)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=VT4P4AT8FTYDL)