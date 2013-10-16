backspace-sign
==============

This repository is for the software stuff of our backspace sign.

To light our sign we bought 3m of the addressable RGB stripes from insomnia lightning. Those stripes have a LPD8086 controller IC for every 2 RGB LEDs.

The server is written in C and running on a Raspberry Pi. It takes a complete frame from UDP on port 10001 and sends it to SPI. Each RGB LED has 3 bytes. Each byte must have a value between 0 to 127 for the brightness. The MSB is ignored.

Sample command line to set all LEDs to white:
perl -e 'print "\xFF\xFF\xFF"x156' | nc -u <IP> 10001

The byte order for the colors is RED, GREEN, BLUE.

More information might be available at http://www.hackerspace-bamberg.de/Schild (german).