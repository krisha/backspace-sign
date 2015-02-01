#include <stdint.h>
#include <linux/spi/spidev.h>
#include <fcntl.h>
#include <sys/ioctl.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <unistd.h>

#include <sys/socket.h>
#include <netinet/in.h>
#include <strings.h>

#define SPI_DEVICE "/dev/spidev0.0"
#define SPI_MODE 0
#define SPI_BITS_PER_WORD 8
#define SPI_SPEED 10000000

#define UDP_PORT 10001
#define LED_COUNT 126
#define FRAME_LEN (LED_COUNT * 3)

void parm_err ( )
{
	fprintf( stderr, "errornous parameter\n" );
	errno = EINVAL;
}

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
		parm_err();
		return -1;
	}
	
	spi->fd = open ( SPI_DEVICE, O_RDWR, 0 );
	
	if ( spi->fd == -1 )
	{
		perror( "failed to open spi device" );
		return -1;
	}
	
	/* set mode 0 (for output only), MSB_FIRST */
	spi->mode = mode;
	if ( ioctl ( spi->fd, SPI_IOC_WR_MODE, &spi->mode ) == -1 )
	{
		perror( "failed to set mode" );
		return -1;
	}
	
	/* set bit_per_word */
	spi->bits_per_word = bits_per_word;
	if ( ioctl ( spi->fd, SPI_IOC_WR_BITS_PER_WORD, &spi->bits_per_word ) == -1 )
	{
		perror( "failed to set bits per word" );
		return -1;
	}
	
	/* set speed */
	spi->speed = speed;
	if ( ioctl ( spi->fd, SPI_IOC_WR_MAX_SPEED_HZ, &spi->speed ) == -1 )
	{
		perror( "failed to set frequency" );
		return -1;
	}
	
	return 0;
}

int spi_tx ( struct spi *spi, uint8_t *data, size_t length )
{
	if ( !spi )
	{
		parm_err ();
		return -1;
	}

	return write ( spi->fd, data, length );
}

int spi_close ( struct spi *spi )
{
	if ( !spi )
	{
		parm_err ();
		return -1;
	}
	return close ( spi->fd );
}

int udp_open ( )
{
	int sockfd;
	
	struct sockaddr_in addr_server;
	
	sockfd = socket ( AF_INET, SOCK_DGRAM, 0 );
	
	if ( sockfd == -1 )
	{
		perror ( "failed to create udp socket" );
		return -1;
	}
	
	bzero ( &addr_server, sizeof ( addr_server ) );
	addr_server.sin_family = AF_INET;
	addr_server.sin_addr.s_addr = htonl ( INADDR_ANY );
	addr_server.sin_port = htons ( UDP_PORT );
	if ( bind ( sockfd, (struct sockaddr *)&addr_server, sizeof ( addr_server ) ) == 1 )
	{
		perror ( "failed to bind udp socket" );
		return -1;
	}
	
	return sockfd;
}

int udp_read_frame ( int sockfd, uint8_t *data )
{
	struct sockaddr addr_client;
	socklen_t addr_len;
	ssize_t rxlen;
	int valid;
	
	if ( !data )
	{
		parm_err ();
		return -1;
	}
	
	addr_len = sizeof ( addr_client );
	rxlen = recvfrom ( sockfd, data, FRAME_LEN, 0, &addr_client, &addr_len);
	if (rxlen == -1)
	{
		perror ( "failed to receive frame" );
		return -1;
	}
	
	valid = rxlen == FRAME_LEN;
	if ( !valid )
	{
		fprintf ( stderr, "ignoring frame of invalid length %zd\n", rxlen );
	}

	if ( sendto ( sockfd, valid ? "\x31" : "\x30", 1, 0, &addr_client,
				sizeof ( addr_client ) ) == -1 )
	{
		perror ( "failed to send frame" );
		return -1;
	}

	return valid-1;
}


int main ( int argc, char *argv[] )
{
	struct spi spi;
	uint8_t *txbuf;
	int sockfd;
	uint8_t r, g, b;
	int i;
		
	if ( spi_open ( &spi, SPI_MODE, SPI_BITS_PER_WORD, SPI_SPEED) == -1 )
		return 1;
		
	sockfd = udp_open ( );
	if ( sockfd == -1 )
		return 2;

	txbuf = malloc ( FRAME_LEN*2 );
	if (!txbuf) {
		fprintf( stderr, "failed to allocate %d bytes\n", FRAME_LEN*2 );
		return 3;
	}
	
	while ( 1 )
	{
		if ( !udp_read_frame ( sockfd, txbuf ) )
		{
			/* zeros */
			for ( i = 0; i < FRAME_LEN; i++ )
				txbuf[FRAME_LEN+i] = 0;
				
			/* change BRG to RGB, set highest bit */
			for ( i = 0; i < FRAME_LEN; i += 3 )
			{
				r = txbuf[i + 0] | 0x80;
				g = txbuf[i + 1] | 0x80;
				b = txbuf[i + 2] | 0x80;
				
				txbuf[i + 0] = b;
				txbuf[i + 1] = r;
				txbuf[i + 2] = g;
			}
			
			spi_tx ( &spi, txbuf, FRAME_LEN*2 );
		}
	}

	/* never reached */
	free ( txbuf );
	close ( sockfd );
	spi_close ( &spi );

	return 0;
}
