<!DOCTYPE html>

<html>
<head>
	<meta charset="utf-8"/>
	<title>backspace Schild</title>

	<meta name="viewport" content="width=device-width, initial-scale=1">

	<link rel="stylesheet" href="//code.jquery.com/mobile/1.4.2/jquery.mobile-1.4.2.min.css" />
	<script src="//ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script>
	<script src="//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/jquery-ui.min.js"></script>
	<script src="//code.jquery.com/mobile/1.4.2/jquery.mobile-1.4.2.min.js"></script>
	
	<!-- see and use http://acko.net/blog/farbtastic-jquery-color-picker-plug-in/ -->
	<script type="text/javascript" src="farbtastic/farbtastic.js"></script>
	<link rel="stylesheet" href="farbtastic/farbtastic.css" type="text/css" />
	
	<script type="text/javascript">
	
	var LEDS_USED = 126;
	var BORDER_DISTANCE = 5;

	/* use load to wait till all CSS values applied */
	$(window).load( function ()
	{
		init();
	});
	
	/* function from http://www.codeproject.com/Articles/355230/HTML-Canvas-A-Simple-Paint-Program-Touch-and-Mou */
	function getPosition ( mouseEvent, eCanvas )
	{
		var x, y;
		if ( mouseEvent.pageX != undefined && mouseEvent.pageY != undefined )
		{
			x = mouseEvent.pageX;
			y = mouseEvent.pageY;
		}
		else
		{
			x = mouseEvent.clientX + document.body.scrollLeft +
				document.documentElement.scrollLeft;
			y = mouseEvent.clientY + document.body.scrollTop +
				document.documentElement.scrollTop;
		}
		
		return { X: x - eCanvas.offsetLeft, Y: y - eCanvas.offsetTop };
	}

	var drawing = false;
	var lastUpdate;
	var updateInProgress = false;
	
	function init ()
	{
		var eCanvas = document.getElementById("paintarea");
		var context = eCanvas.getContext ( "2d" );
		
		/* reset the canvas size to CSS size */
		eCanvas.width = parseFloat ( $('#paintarea').css ( "width" ) );
		eCanvas.height = parseFloat ( $('#paintarea').css ( "height" ) );
		
		context.lineWidth = 20;
		
		clear ( eCanvas, context );
		
		lastUpdate = +new Date();

		$(document).on ( "vmousedown", function ( mouseEvent )
		{
			var position = getPosition (mouseEvent, eCanvas);
			
			drawing = true;
			
			context.moveTo(position.X, position.Y);
			context.beginPath();
		});
		
		$(document).on ( "vmousemove", function ( mouseEvent )
		{
			if ( drawing )
			{
				drawLine ( mouseEvent, eCanvas, context );
				//drawLogo ( context );
				
				if ( +new Date() - lastUpdate > 250 )
					updateSign ( eCanvas, context, false );
			}
		});
		
		$(document).on ( "vmouseup", function ( mouseEvent )
		{
			if ( drawing )
			{
				drawFinish ( mouseEvent, eCanvas, context );
				//drawLogo ( context );
				updateSign ( eCanvas, context, false );
			}
				
			drawing = false;
		});

		/* prevent scrolling, else painting not working */
		document.body.addEventListener('touchmove', function(e)
		{
			e.preventDefault();
		});
		
		$("#btnclear").click ( function ()
		{
			/* execute after vmouseup event */
			setTimeout ( function ()
			{
				clear ( eCanvas, context );
				updateSign ( eCanvas, context, true );
			}, 200 );
		});
		
		var f = $.farbtastic('#colorpicker').linkTo ( function ( color )
		{
			$('#btncolor').css ( "background-color", "#" + color );
			context.strokeStyle = color;
			
		});
		
		$("#btncolor").click ( function ()
		{
			$('#colorpicker').toggle();
		});
		
		$('#colorpicker').hide();
		
		f.setColor ( "#FF0000" );
	}
	
	var logo;
	
	function drawLogo ( context )
	{
	
		context.clearRect ( 50, 50, 200, 200 );
		
		if (!logo)
		{
			logo = new Image();
			logo.src = "logo.png";
			logo.width = "200px";
			
			logo.onload = function ()
			{
				context.drawImage ( logo, 50, 50, 200, 200 );
			};
		}
		else
			context.drawImage ( logo, 50, 50, 200, 200 );
	}

	function clear ( eCanvas, context )
	{
		//var img = new Image();
		var oldColor = context.strokeStyle;
		var oldLineWidth = context.lineWidth;
		
		context.clearRect ( 0, 0, eCanvas.width, eCanvas.height );
		
		context.lineWidth = 5;
		context.strokeStyle = "black";
		context.strokeRect ( BORDER_DISTANCE, BORDER_DISTANCE,
			eCanvas.width - 2 * BORDER_DISTANCE, eCanvas.height - 2 * BORDER_DISTANCE );

		drawLogo ( context );
		
		context.strokeStyle = oldColor;
		context.lineWidth = oldLineWidth;
	}
	
	function hex ( val )
	{
		return String.fromCharCode ( parseInt ( val ) );
	}
	
	function updateSign ( eCanvas, context, force )
	{
	
		if ( updateInProgress && !force )
			return;
		updateInProgress = true;
			
		var i, r, g, b;
		var arr = [];
		
		var w_inner = eCanvas.width - 2 * BORDER_DISTANCE;
		var h_inner = eCanvas.height - 2 * BORDER_DISTANCE;
		
		var data = context.getImageData ( BORDER_DISTANCE, BORDER_DISTANCE,
			w_inner, h_inner );
			
		var leds_per_side = LEDS_USED / 4;
		var led_every_pixel_h = parseInt ( h_inner / leds_per_side );
		var led_every_pixel_w = parseInt ( w_inner / leds_per_side );
		
		/* right side, fix rounding by 1 */
		for ( i = 0; i < leds_per_side-1; i++ )
		{
			r = data.data[(w_inner-1+w_inner*led_every_pixel_h*i)*4+0];
			g = data.data[(w_inner-1+w_inner*led_every_pixel_h*i)*4+1];
			b = data.data[(w_inner-1+w_inner*led_every_pixel_h*i)*4+2];
			
			arr.push ( [ r, g, b ] );
		}
		
		/* bottom side */
		for ( i = 0; i < leds_per_side; i++ )
		{
			r = data.data[((h_inner-1)*w_inner+w_inner-1-led_every_pixel_w*i)*4+0];
			g = data.data[((h_inner-1)*w_inner+w_inner-1-led_every_pixel_w*i)*4+1];
			b = data.data[((h_inner-1)*w_inner+w_inner-1-led_every_pixel_w*i)*4+2];
			
			arr.push ( [ r, g, b ] );
		}

		/* left side, fix rounding by 1 */
		for ( i = 0; i < leds_per_side-1; i++ )
		{
			r = data.data[((h_inner-1-i*led_every_pixel_h)*w_inner)*4+0]
			g = data.data[((h_inner-1-i*led_every_pixel_h)*w_inner)*4+1]
			b = data.data[((h_inner-1-i*led_every_pixel_h)*w_inner)*4+2]
			
			arr.push ( [ r, g, b ] );
		}
		
		/* top side */
		for ( i = 0; i < leds_per_side; i++ )
		{	
			r = data.data[i*led_every_pixel_w*4+0];
			g = data.data[i*led_every_pixel_w*4+1];
			b = data.data[i*led_every_pixel_w*4+2];
			
			arr.push ( [ r, g, b ] );
		}
		
		var json = JSON.stringify ( { data: arr } );

		//$('#data').text ( json );
		$.post ( "submit.php", { data: json }, function (d)
		{
			//$('#data').text ( d );
			updateInProgress = false;
		});
		
		lastUpdate = +new Date();
	}
	
	function drawLine ( mouseEvent, eCanvas, context )
	{
		var position = getPosition ( mouseEvent, eCanvas );
		
		context.lineTo ( position.X, position.Y );
		context.stroke();
	}
	
	function drawFinish ( mouseEvent, eCanvas, context )
	{
		drawLine ( mouseEvent, eCanvas, context );
		context.closePath();
	}
	
	</script>
	
	<style>
		#main {
			max-width: 300px;
			margin: auto;
			padding: 20px;
			text-align:center;
		}
		
		#paintarea {
			border: 1px solid black;
			width: 100%;
			max-width: 300px;
			height: 300px;
		}
		
		#colorpicker {
			margin: auto;
			width: 195px;
		}
		
		#info {
			-webkit-touch-callout: none;
			-webkit-user-select: none;
			-khtml-user-select: none;
			-moz-user-select: none;
			-ms-user-select: none;
			user-select: none;
		}
	</style>
</head>

<body>
	<div id="main">
		<div id="colorpicker"></div>
		<div class="ui-grid-a">
			<div class="ui-block-a"><input class="btn" type="button" id="btncolor" value="Color"/></div>
			<div class="ui-block-b"><input class="btn" type="button" id="btnclear" value="Clear"/></div>
		</div>
		<canvas id="paintarea"></canvas>
		<div id="info">Paint on the image! <a href="http://www.hackerspace-bamberg.de/Schild#HTML5_Client" target="_blank">Info.</a></div>
	</div>
	
	<div id="data"></div>
</body>
</html>
