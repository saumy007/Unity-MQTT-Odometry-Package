## 1. Overview

This documentation is about the application for Meta Quest to publish the topic of Odometry using Meta Quest 3.

This SOP describes the standard procedure for connecting the Meta Quest VR application (newapp_6s) to an MQTT broker running on a Linux machine using Mosquitto with a UI where user can enter the IP.

Previously, the Quest APK required rebuilding whenever the IP of the broker changed (Booster mobile hotspot IP changes frequently).

This eliminates the need to rebuild APKs when the network IP changes.

---

## 2. System Overview

| Component | Role |
| --- | --- |
| Meta Quest 2/3 | Publishes pose/odometry data |
| Linux PC/Laptop | Runs Mosquitto broker |
| Booster_Phone hotspot | Shared Wi-Fi network for Quest + Linux |
| Unity APK (newapp_6s) | Publishes on MQTT topic "test" |

APK Name: **newapp_60s.apk**

---

## 3. Meta Quest Setup

### 3.1 Prepare Quest via SideQuest

1. Connect Quest via USB-C
2. Connect both Linux PC and Meta Quest to the same WiFi (e.g., *Booster Phone*)
3. Enable Developer Mode (Meta App → Devices → Developer Mode (already enabled but can check))
4. Approve USB Debugging inside headset
5. Launch SideQuest application
6. Turn off Proximity Sensor and Disable Guardian in SideQuest settings
7. Install & run the application on Meta Quest
8. On Linux, check IP address:
9. `hostname -I`
10. Enter this IP in the Quest UI and click **Connect**

### 3.2 Run Linux Command to Verify Data

`mosquitto_sub -h <ip address> -t "test" -v`

---

## 4. Linux System Setup

### 4.1 Verify Required Services

Ensure MQTT Broker is installed and Booster Phone Hotspot is running.

Check Mosquitto:

`sudo systemctl status mosquitto`

If not running:

`sudo systemctl start mosquitto
sudo systemctl enable mosquitto`

---

## 5. Install & Configure Mosquitto Broker

### 5.1 Install Mosquitto

`sudo apt update
sudo apt install mosquitto mosquitto-clients -y`

### 5.2 Verify Broker Status

`sudo systemctl status mosquitto`

Expected: **active (running)**

### 5.3 Configure Mosquitto for Quest Connectivity

Open config:

`sudo nano /etc/mosquitto/mosquitto.conf`

Add or ensure the following lines:

`listener 1883
allow_anonymous true
autosave_interval 1800
persistence true`

`0 0.0.0.0:1883 LISTEN mosquitto`

---

## 6. Testing MQTT on Linux

### 6.1 Subscribe to Meta Quest Topic

`mosquitto_sub -h <IP Address connected to Booster Phone> -t "test"`

---

## 7. Meta Quest Application Usage

### 7.1 Recommended Headset Settings

Inside Quest:

- Settings → Developer → **Enable Proximity Sensor**
- Settings → Power → **Disable Auto-Sleep / Standby**
- Ensure Wi-Fi → connected to **Booster_Phone**

### 7.2 Launch Application

Open: **Apps → Unknown Sources → newapp_6s**

The application automatically resolves the broker using Mosquitto and begins publishing odometry data continuously to topic **"test"**.

### Expected Behavior

**Quest UI** → Connects successfully

**Linux Terminal** → Shows continuous data stream:

`{"position":{"x":...,"y":...,"z":...}, "rotation":{"x":...,"y":...,"z":...,"w":...}}`

# Procedure to build the APK in Unity

Open Unity Hub click on Add

Click on Add project from the disk

Add the directory where these 3 files are located

- Assets
- Packages
- ProjectSettings

Load up the Unity Project

Click on Build Settings and Switch to Android

Check on the Scene List if the desired Scene is in the list as in our case it is HandsDemoScene

Click on Build (this will build an APK)

Install the APK on meta quest through adb

`sudo apt install adb
adb devices #check devices
adb install "name of your apk".apk`

The App will be installed in the meta quest in unknown sources with your desired name.
