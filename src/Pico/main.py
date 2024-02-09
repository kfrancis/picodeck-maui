import json
import picokeypad
import time
import network
import socket
import ujson

keypad = picokeypad.PicoKeypad()
keypad.set_brightness(1.0)  # Maximum brightness for button presses
NUM_PADS = keypad.get_num_pads()
last_button_states = 0
context_switch_mode = False  # True if next button press selects an application
current_application_key = None  # Use to store the key of the current application

# Load config
try:
    with open("config.json", "r") as file:
        config = json.load(file)
except Exception as e:
    log_exception(e)  # Assuming log_exception is implemented elsewhere
    
ssid = config["network_name"]
password = config["network_password"]

# Connect to Wi-Fi
wlan = network.WLAN(network.STA_IF)
wlan.active(True)
wlan.connect(ssid, password)

# Wait for connection
while not wlan.isconnected():
    pass

print(f"Connected to Wi-Fi {ssid}")
    
def send_message_to_server(message, host, port):
    addr_info = socket.getaddrinfo(host, port)[0][-1]
    s = socket.socket()
    try:
        s.connect(addr_info)
        s.send(message)
        print("Sent message to the server:", message)
    finally:
        s.close()

# Example usage
host = config['host']
port = config['port']

def draw_red_x():
    """
    Blink specific keys (0, 3, 5, 6, 9, 10, 12, 15) to display a red 'X' twice.
    Keys are assumed to be in a 4x4 grid and labeled from 0 to F.
    Each blink is 500ms on, followed by 500ms off, repeated twice.
    """
    red_x_keys = [0, 3, 5, 6, 9, 10, 12, 15]  # Decimal indexes for keys to be red
    off_color = (5, 5, 5)  # Dim color for off state
    
    def illuminate_x(is_on):
        color = (255, 0, 0) if is_on else off_color
        for i in range(NUM_PADS):
            if i in red_x_keys:
                keypad.illuminate(i, *color)  # Set color based on is_on flag
            else:
                keypad.illuminate(i, *off_color)  # Always dim others
        keypad.update()
    
    for _ in range(2):  # Blink twice
        illuminate_x(True)  # Turn on the red 'X'
        time.sleep(0.5)  # 500ms on
        illuminate_x(False)  # Turn off the red 'X'
        time.sleep(0.5)  # 500ms off

    # After blinking, return to the application selection state
    set_context_switch_mode()

def illuminate_key(index, r, g, b, brightness=1.0):
    keypad.set_brightness(brightness)  # Set brightness for individual key illumination
    keypad.illuminate(index, r, g, b)
    keypad.update()
    keypad.set_brightness(1.0)  # Reset to maximum brightness for subsequent operations

def illuminate_keys_default(brightness=0.1):
    keypad.set_brightness(brightness)  # Reduced brightness for background
    for i in range(NUM_PADS):
        keypad.illuminate(i, 5, 5, 5)  # Default dim color
    keypad.update()

def illuminate_application_keys():
    global current_application_key
    if current_application_key is not None:
        off_color = config["applications"][current_application_key]["offColor"]
        keypad.set_brightness(0.1)  # Reduced brightness for application "off" state
        for i in range(NUM_PADS):
            keypad.illuminate(i, *off_color)
        keypad.update()

def set_context_switch_mode():
    global context_switch_mode
    context_switch_mode = True
    illuminate_keys_default(1.0)  # Full brightness to indicate mode switch

def select_application(button):
    global context_switch_mode, current_application_key
    context_switch_mode = False
    # Find application by button key or fallback to index
    for index, app in enumerate(config["applications"]):
        if app.get("key", str(index)) == str(button):
            current_application_key = index
            print(f"Application switched to {app['name']}")
            illuminate_application_keys()
            return
    # If no application is found for the button, display red 'X'
    print("No application for selected button, displaying red 'X'")
    draw_red_x()

def execute_shortcut(button_index):
    if current_application_key is not None:
        app = config["applications"][current_application_key]
        shortcut = app["shortcuts"].get(str(button_index))
        if shortcut:
            shortcut_name = shortcut.get("name", "Unnamed Shortcut")
            print(f"Executing {shortcut_name} for {app['name']}")
            
            message_dict = {
                "application": app['name'],
                "shortcut_name": shortcut_name,
                "keys": shortcut['keys']
            }
            
            # Serialize the dictionary to a JSON string
            message_json = ujson.dumps(message_dict)
            
            # Send the serialized message to the server
            send_message_to_server(message_json, host, port)
            
            illuminate_key(button_index, *shortcut["color"], brightness=1.0)
            time.sleep(0.5)  # Briefly show the color at full brightness
            illuminate_application_keys()

# Setup the default state
illuminate_keys_default()

# Main loop
while True:
    button_states = keypad.get_button_states()
    if button_states != last_button_states:
        for button in range(NUM_PADS):
            if button_states & (1 << button) and not last_button_states & (1 << button):
                if context_switch_mode:
                    select_application(button)
                elif button == 15:  # Custom key for switching context
                    set_context_switch_mode()
                else:
                    execute_shortcut(button)
        last_button_states = button_states
    time.sleep(0.1)
