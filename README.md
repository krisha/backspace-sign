backspace-sign
==============

This repository is for the software stuff of our backspace sign.

To light our sign we bought 3m of the addressable RGB stripes from insomnia lightning. Those stripes have a LPD8086 controller IC for every 2 RGB LEDs.

The server is running on a Raspberry Pi, takes a complete frames from UDP on port 10001 and sends it to SPI. Each RGB LED takes 3 bytes. Each byte must have the MSB bit set (logical OR with 0x80). So the brightness can be regulated in upto 127 steps.

Sample command line to set all LEDs to white:
perl -e 'print "\xFF\xFF\xFF"x156' | nc -u <IP> 10001

The byte order for the colors is BLUE, RED, GREEN.

More information might be available at http://www.hackerspace-bamberg.de/Schild (german).