<?php

$server = "schild.core.bckspc.de";
$port = 10004;

if ( !isset ( $_POST['data'] ) )
	die ( "err: no data" );
	
$data = json_decode ( $_POST['data'], true );
$data = $data["data"];

if ( count ( $data ) == 126 )
{
	$fp = fsockopen ( "udp://" . $server, $port, $errno, $errstr, 2 );
	
	if ( !$fp )
		echo "err: $errstr ($errno)";
	else
	{
		$rawData = "";
		
		for ( $i = 0; $i < 126; $i++ )
			for ( $j = 0; $j < 3; $j++ )
				$rawData .= chr ( $data[$i][$j] / 2 );
				
		fwrite ( $fp, $rawData, 126*3 );
		echo "ok";
	}
}
else
	die ( "err: wrong length" );

?>