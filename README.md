# picodeck-maui

![PicoDeck](picodeck.png)

## What is this?

My son is a streamer, or aspires to be. I'm not forking out the kind of cash that those crazy top-end devices cost for this hobby, so I thought I could make one myself. I bought the [hardware](https://shop.pimoroni.com/products/pico-rgb-keypad-base?variant=32369517166675) and picked up a Pico W for this project.

## How does this work generally?

### Pico W
- The pico is connecting to wifi using the supplied network/password
- The keypad is setup so that key F (15) is like the menu button. Selecting it lets you pick an "application" which can have 15 different shortcuts you can specify.
- Shortcut keys should map to the virtual key (at least atm as I'm using InputSimulator) so it can be parsed.

### Companion/Taskbar App
- The taskbar app talks to a Windows Service which does the input simulation using IPC

### MAUI App
- This is how the user might? configure the app? Might just make sense to make the companion app do all this.

## Todo:

- [X] Design a structure for the config.json to make this easily configurable
- [X] Find a way to connect the pico to the host machine
- [ ] Design the maui app to allow easy config (by reading/writing the config file to/from the pico)

## Hardware:
- https://shop.pimoroni.com/products/pico-rgb-keypad-base?variant=32369517166675
