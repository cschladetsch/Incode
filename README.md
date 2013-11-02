Incode
======

Custom input system for keyboard and mouse for Windows.

This application allows the user to control the mouse and other systems such as cursor and scrolling
using a minimal set of keys.

Hold an 'Override' button - in my case on a 88-Key WASD keyboard, it's the Amiga-like second backslash key beside the
left shift key - and then use the rest of the keyboard to send custom input.

For instance, press and hold the Override button, and use ESDF to move the mouse cursor up down left right. Movement
is passed through a customised filter to emulate the behavior of an Ibm ThinkPad clit button.

W and R keys send scroll up and down, again with a custom filter and timings. 

The space bar simulates the left mouse button, and 'g' the right. 'v' sends enter, and there are many others.

There are many other ins and outs. This is not meant to be a system for a typical user. It's very hard-coded to
me and my setup (custom WASD keyboard, logitech touchpad, vi, no mouse).

That said, it is readily customisable if you can read C#. I haven't made it generic because I wrote it for me. 
But you are welcome to see what I did and how and make it your own.

Feel free to contact me with comments, suggestions or bugs.

Cheers,
Christian

christian.schladetsch@gmail.com
