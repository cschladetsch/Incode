# InCode ![Icon](Doc/Logo.png "Incode Logo")
[![CodeFactor](https://www.codefactor.io/repository/github/cschladetsch/incode/badge)](https://www.codefactor.io/repository/github/cschladetsch/incode) [![License](https://img.shields.io/github/license/cschladetsch/incode.svg?label=License&maxAge=86400)](./LICENSE) [![Release](https://img.shields.io/github/release/cschladetsch/incode.svg?label=Release&maxAge=60)](https://github.com/cschladetsch/incode/releases/latest)

Custom input system for keyboard and mouse for Windows.

This application allows the user to control the mouse and other systems such as cursor and scrolling using a minimal set of keys. At worst, it is a great replacement for the Microsoft "MouseKeys" system.

Hold an 'Override' button - By default the Right Control key - and then use the rest of the keyboard to send custom input.

Use the provided `SharKeys` binary to remap the CapsLock key to the Right Control Key.

For instance, press and hold the Override button, and use ESDF to move the mouse cursor up down left right. Movement is passed through a customised filter to emulate the behavior of an Ibm ThinkPad clit button.  

R and V keys send scroll up and down, again with a custom filter and timings. 

The space bar simulates the left mouse button, and C the right mouse button.

There is some customisation available via a simple GUI, but the best way is to just edit the code. It's not great code, but the configuration itself is quite straight-forward.

Feel free to contact me with comments, suggestions or bugs.
