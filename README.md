# REPO Death Capture

Uses Steam's new Game Recording feature to capture your best deaths.

# Requirements

You **must** manually [enable](https://help.steampowered.com/en/faqs/view/23B7-49AD-4A28-9590#6) **Record in Background** in the Steam client.

That's it, you're done.

## Recommendations

You should also turn on **Record Microphone** so that Steam can capture any hillarity that ensues.

**Automatic Gain Control** may be useful but can produce crackly audio depending on your setup. If you plan to use this to capture clips long term, I recommend jumping into a save solo and testing with AGC on/off.

## Configuration

By default, the Steam overlay will open after a death occurs; there is a short delay to facilitate a more natural reaction to whatever caused your demise.

You can disable this behavior in the configuration menu if desired, just remember that if your recording length is low you will need to check and export any clips you wish to keep at some point.

## Other Features

Recordings are labelled based on the state of the game:
- Exploring [map name]
- Shopping
- Deathmatch

As Steam adjusts the "Timeline" API I will look into adding more detail.