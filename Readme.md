# InCode ![Icon](Doc/Logo.png "Incode Logo")
[![CodeFactor](https://www.codefactor.io/repository/github/cschladetsch/incode/badge)](https://www.codefactor.io/repository/github/cschladetsch/incode) [![License](https://img.shields.io/github/license/cschladetsch/incode.svg?label=License&maxAge=86400)](./LICENSE) [![Release](https://img.shields.io/github/release/cschladetsch/incode.svg?label=Release&maxAge=60)](https://github.com/cschladetsch/incode/releases/latest)

Custom input system for keyboard and mouse for Windows.

This application allows the user to control the mouse and other systems such as cursor and scrolling using a minimal set of keys - without actually using the mouse. It also allows for the use of abbreviations. Yes, there are other tools that do this and better but hey.

## Usage
Hold an 'Interrupt' button - By default the Right-Control key - and then use the rest of the keyboard to send custom input.

*NOTE* Use the provided `SharpKeys` binary to remap the CapsLock key to the Right-Control Key. The CapsLock key is an anacronism in any case, may as well put it to good use.

For instance, press and hold the `Interrupt` button, and:
* use `ESDF` to move the mouse cursor up down left right. Movement is passed through a customised filter to emulate the behavior of an Ibm ThinkPad clit button.  
* `R` and `V` keys send scroll up and down, again with a custom filter and timings. 
* The `space bar` simulates the left mouse button
* `G` the right mouse button.
* You can also double-click. I think the threshold is 300ms.
* Double-pressing the `Interrupt` key centers the cursor on the main monitor.
* There is some customisation available via a simple GUI, but to change the keys the only current way is to just edit the code.


## Installation
Just build the Visual Studio solution. This is very much *not* a cross-platform app so I didn't bother with CMake.

## Abbreviations
Enter abbreviation mode with `Interrupt`-Q (typically Capslock-Q). Then you can insert any text as mapped in the config file.

For instance, my email address is `christian@schladetsch.com`. I end up typing that a *lot*. Now I can enter it with `Interrupt`-Qg.

The `g` is the abbreviation stored in the Json file. I've added a bunch of things I use a lot, like passwords etc. Obviously you'll have to make your own Config.json in the same folder as the IncodeWindow.exe executable.

When you enter abbreviation mode, a popup appears showing all your abbreviations. Note that you need to still hold down the `Interrupt` key to complete the abbreviation.

## Json Config
The configuration is stored in `Config.json`, in the same folder as the app.

A typical config file would look like:

```json
{
  "Abbreviations":
  {
    "g":  "christian.schladetsch@gmail.com",
    "v": "christian@schladetsch.com",
    "p":  "04712341234",
    "gp": "+61(0)37234524",
    "cc":  "1234123412341234",
    "p1": "password",
    "p2": "hunter11",
    "p3": "not-telling",
    "ad": "29 Fuddle St, East Place, Somewhere, Country, 3002"
  },
  "Speed": 250,
  "Accel": 12,
  "ScrollScale": 0.5,
  "ScrollAccel": 1.15,
  "ScrollAmount": 3
}

```

## Bugs or Requests
Use GitHub's Issues to raise any faults or feature requests.

Othweose, feel free to [contact me](mailto:christian@schladetsch.com).
