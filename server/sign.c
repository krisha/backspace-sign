#include <stdint.h>
#include <linux/spi/spidev.h>
#include <fcntl.h>
#include <sys/ioctl.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

#include <sys/socket.h>
#include <netinet/in.h>
#include <strings.h>

#define LED_COUNT 156

struct spi
{
	int fd;
	uint8_t mode;
	uint8_t bits_per_word;
	uint32_t speed;
};

int spi_open ( struct spi *spi, uint8_t mode, uint8_t bits_per_word, uint32_t speed )
{
	if ( !spi )
	{
		printf ( "errornous parameter\n" );
		return -1;
	}
	
	spi->fd = open ( "/dev/spidev0.0", O_RDWR, 0 );
	
	if ( spi->fd == -1 )
	{
		printf ( "could not open device\n" );
		return -1;
	}
	
	/* set mode 0 (for output only), MSB_FIRST */
	spi->mode = mode;
	if ( ioctl ( spi->fd, SPI_IOC_WR_MODE, &spi->mode ) == -1 )
	{
		printf ( "failed to set mode\n" );
		return -1;
	}
	
	/* set bit_per_word */
	spi->bits_per_word = bits_per_word;
	if ( ioctl ( spi->fd, SPI_IOC_WR_BITS_PER_WORD, &spi->bits_per_word ) == -1 )
	{
		printf ( "failed to set bits per word\n" );
		return -1;
	}
	
	/* set speed */
	spi->speed = speed;
	if ( ioctl ( spi->fd, SPI_IOC_WR_MAX_SPEED_HZ, &spi->speed ) == -1 )
	{
		printf ( "error setting frequency\n" );
		return -1;
	}
	
	return 0;
}

void spi_close ( struct spi *spi )
{
	if ( !spi )
		return;
		
	close ( spi->fd );
}

int spi_tx ( struct spi *spi, uint8_t *data, size_t length )
{
	if ( !spi )
	{
		printf ( "error writing SPI data\n" );
		return -1;
	}
	
	return write ( spi->fd, data, length );
}

int udp_open ( )
{
	int sockfd;
	
	struct sockaddr_in addr_server;
	
	sockfd = socket ( AF_INET, SOCK_DGRAM, 0 );
	
	if ( sockfd == -1 )
	{
		printf ( "udp opening failed\n" );
		return -1;
	}
	
	bzero ( &addr_server, sizeof ( addr_server ) );
	addr_server.sin_family = AF_INET;
	addr_server.sin_addr.s_addr = htonl ( INADDR_ANY );
	addr_server.sin_port = htons ( 10001 );
	if ( bind ( sockfd, (struct sockaddr *)&addr_server, sizeof ( addr_server ) ) == 1 )
	{
		printf ( "udp error binding\n" );
		return -1;
	}
	
	return sockfd;
}

int udp_read_frame ( int sockfd, uint8_t *data )
{
	struct sockaddr addr_client;
	socklen_t len;
	int rxlen;
	int valid;
	
	if ( !data )
	{
		printf ( "!data\n" ); 
		return -1;
	}
	
	len = sizeof ( addr_client );
	rxlen = recvfrom ( sockfd, data, LED_COUNT*3, 0, &addr_client, &len );
	
	valid = rxlen == LED_COUNT * 3;

	sendto ( sockfd, valid ? "\x31" : "\x30", 1, 0, &addr_client, sizeof ( addr_client ) );
		
	return valid-1;

}


int main ( int argc, char *argv[] )
{
	struct spi spi;
	uint8_t *txbuf;
	int sockfd;
		
	if ( spi_open ( &spi, 0, 8, 20000000  ) == -1 )
		return -1;
		
	sockfd = udp_open ( );
	if ( sockfd == -1 )
		return -1;
	
	txbuf = malloc ( LED_COUNT*3*2 );
	
	while ( 1 )
	{
		if ( !udp_read_frame ( sockfd, txbuf ) )
		{
			/* zeros */
			for ( i = 0; i < LED_COUNT*3; i++ )
				txbuf[LED_COUNT*3+i] = 0;
			
			spi_tx ( &spi, txbuf, LED_COUNT*3*2 );
			
			/* can be removed */
			usleep ( 10 * 1000);
		}
	}

	/* never reached */
	free ( txbuf );
	spi_close ( &spi );

	return 0;
}
