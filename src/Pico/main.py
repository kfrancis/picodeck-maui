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
host = None

def validate_config(config):
    required_fields = ["network_name", "network_password", "port", "applications"]

    # Check for missing required fields
    missing_fields = [field for field in required_fields if field not in config]
    if missing_fields:
        raise ValueError(f"Missing required configuration fields: {', '.join(missing_fields)}")

    # Check for specific types or formats if necessary
    if not isinstance(config["network_name"], str) or not config["network_name"]:
        raise ValueError("Invalid or empty 'network_name' in configuration.")
    if not isinstance(config["network_password"], str) or not config["network_password"]:
        raise ValueError("Invalid or empty 'network_password' in configuration.")
    if not isinstance(config["host"], str) or not config["host"]:
        raise ValueError("Invalid or empty 'host' in configuration.")
    if not isinstance(config["port"], int) or config["port"] <= 0:
        raise ValueError("Invalid or out of range 'port' in configuration.")

    # Validate the 'applications' field
    if not isinstance(config["applications"], list) or not config["applications"]:
        raise ValueError("'applications' should be a non-empty list in configuration.")

    for app in config["applications"]:
        if "name" not in app or not isinstance(app["name"], str):
            raise ValueError("Each application must have a valid 'name'.")
        if "shortcuts" not in app or not isinstance(app["shortcuts"], dict):
            raise ValueError("Each application must have a 'shortcuts' dictionary.")



# Function to connect to Wi-Fi with timeout and error handling
def connect_to_wifi(wlan, ssid, password, timeout_seconds=10):
    wlan.active(True)
    wlan.connect(ssid, password)

    start_time = time.time()
    while not wlan.isconnected():
        if time.time() - start_time > timeout_seconds:
            print(f"Failed to connect to Wi-Fi {ssid} within {timeout_seconds} seconds.")
            return False
        time.sleep(0.1)  # Sleep to avoid busy waiting
    print(f"Connected to Wi-Fi {ssid}")
    return True

def send_message_to_server(message, server_ip, port):
    try:
        # Try to get the address info for the host and port
        addr_info = socket.getaddrinfo(server_ip, port)[0][-1]
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            # Attempt to connect and send the message
            s.connect(addr_info)
            s.send(message.encode('utf-8'))  # Ensure the message is bytes
            print("Sent message to the server:", message)
        except OSError as e:
            print("Socket error occurred while sending message to the server. Error:", e)
        finally:
            s.close()
    except Exception as e:
        print("An unexpected error occurred while sending message to the server. Error:", e)

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

def listen_for_server():
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    client_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    client_socket.bind(("", 9999))

    print("Listening for server broadcasts...")
    while True:
        message, server_address = client_socket.recvfrom(1024)
        print(f"Discovered server at {server_address[0]} with message: {message.decode()}")

        # Return the discovered server IP
        return server_address[0]

def acknowledge_server(server_ip):
    global host  # Declare 'host' as global to modify the global variable
    ack_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        ack_socket.sendto(b'Client Acknowledgment', (server_ip, 10000))
        print("Sent acknowledgment to server.")
    finally:
        ack_socket.close()  # Ensure the socket is closed

    # Update the global 'host' variable
    host = server_ip

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

# Load config
try:
    with open("config.json", "r") as file:
        config = ujson.load(file)
    validate_config(config)
except ValueError as e:
    print(f"Configuration Error: {e}")
except Exception as e:
    print(f"Unexpected Error: {e}")

ssid = config["network_name"]
password = config["network_password"]
port = config['port']

wlan = network.WLAN(network.STA_IF)
if not connect_to_wifi(wlan, ssid, password):
    # Handle connection failure (e.g., retry, fallback, or halt)
    print("Unable to establish Wi-Fi connection. Check configuration or network status.")

# Wait until the server is discovered and acknowledged
host = None
while host is None:
    host = listen_for_server()
    acknowledge_server(host)

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


