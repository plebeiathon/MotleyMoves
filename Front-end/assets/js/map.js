mapboxgl.accessToken = "...";

var event_info;
var lngLat;
var marker;
var profile;
var attendees;
var race_marker_count = 0;
var starting_point = [-120.9950321238352, 37.63972546309286];  //Assume Centering on Modesto
var map_loaded = false
var map_content_loaded = false;
var checked_in = false;
var event_occuring = false;
var points = {
	"type": "FeatureCollection",
	"features": []
};
var trace = {
	"type": "FeatureCollection",
	"features": [{
		"type": "Feature",
		"geometry": {
			"type": "LineString",
			"coordinates": []
		}
	}]
};
var icon = { "Race Marker": "marker", "Bathrooms": "toilet", "Refreshments": "beer", "Parking": "car", "Person": "marker" };
var save_points = [];
var load_points = [];

var map = new mapboxgl.Map({ container: 'map', style: 'mapbox://styles/mapbox/dark-v10', center: starting_point, zoom: 8 });
var geolocate = new mapboxgl.GeolocateControl({ positionOptions: { enableHighAccuracy: true }, trackUserLocation: true });
map.addControl(geolocate);
geolocate.on('geolocate', function (e) {
	if (event_occuring) {
		if (checked_in) {
			$.post(
				".../api/updatePosition" +
				"?googleID=" + profile.getId() +
				"&eventID=" + get_url_parameter("name") +
				"&lat=" + e.coords.latitude +
				"&lng=" + e.coords.longitude,
				JSON.stringify({}),
				function (result) {
				}
			);
		}
	}
});

$.ajax({
	type: "GET",
	url: ".../api/getEvent?name=" + get_url_parameter("name"),
	success: function (result) {
		event_info = JSON.parse(result);

		var day_lookup = ["Sunday", "Monday","Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
		var month_lookup = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
		var now = new Date();
		var then = new Date(event_info.start);
		then.setDate(then.getDate() + 1);

		if (now.getFullYear() == then.getFullYear() && now.getMonth() == then.getMonth() && now.getDate() == then.getDate()) {
			event_occuring = true;
		}
		console.log(event_info + " " + now + " " + event_occuring);

		document.getElementById("event_title").textContent = event_info.title;
		document.getElementById("event_description").textContent = event_info.description;
		document.getElementById("event_registered").textContent = " Attendees: " + event_info.numRegistered;
		document.getElementById("event_date").textContent = " " + day_lookup[then.getDay()] + ", " + month_lookup[then.getMonth()] + " " + then.getDate() + ", " + then.getFullYear();
	}
});

$.ajax({
	type: "GET",
	url: ".../api/getRacePoints?name=" + get_url_parameter("name"),
	success: function (result) {
		load_points = JSON.parse(result).points;
		map_content_loaded = true;
		if (map_loaded) populateMap();
	}
});

function populateMap() {
	/* race lines */
	map.addSource('trace', { type: 'geojson', data: trace });
	map.addLayer({
		"id": "trace",
		"type": "line",
		"source": "trace",
		"paint": {
			"line-color": "#47D3E5",
			"line-opacity": 1,
			"line-width": 8
		}
	});
	/* other points */
	map.addSource('points', { type: 'geojson', data: points });
	map.addLayer({
		"id": "points",
		"type": "symbol",
		"source": 'points',
		"layout": {
			"icon-image": ["concat", ["get", "icon"], "-15"],
			'icon-size': 2,
			"text-field": ["get", "title"],
			"text-font": ["Open Sans Semibold", "Arial Unicode MS Bold"],
			"text-offset": [0, 0.9],
			"text-anchor": "top",
			"icon-allow-overlap": true,
			"icon-ignore-placement": true,
			"text-allow-overlap": true
		},
		"paint": {
			"text-color": "#ffffff"
		}
	});
	map.flyTo({ center: starting_point, zoom: 10, bearing: 0, speed: 3, curve: 1 });
	setTimeout(function () { /* Animates loading of points and lines */
		var i = 0;
		var timer = window.setInterval(function () {
			if (i < load_points.length) {
				point_coordinates = [load_points[i].lng, load_points[i].lat];
				if (load_points[i].type == "Race Marker") {

					/* Add line segment and update source */
					race_marker_count++;
					if (race_marker_count == 1) {
						starting_point = point_coordinates;
						text = "Race Start";
						points.features.push({
							"type": 'Feature',
							"geometry": {
								"type": 'Point',
								"coordinates": point_coordinates
							},
							"properties": {
								"title": text,
								"icon": icon["marker"],
							}
						});
						map.getSource('points').setData(points);
					}
					trace.features[0].geometry.coordinates.push(point_coordinates);
					map.getSource('trace').setData(trace);
					map.panTo(point_coordinates);
				} else {
					/* Add icon and update source */
					points.features.push({
						"type": 'Feature',
						"geometry": { "type": 'Point', "coordinates": point_coordinates },
						"properties": { "icon": icon[load_points[i].type] }
					});
					map.getSource('points').setData(points);
				}
				i++;
			} else {
				window.clearInterval(timer);
			}
		}, 1000 / load_points.length);
	}, 1000);
}

function addPoint() {
	var type = document.getElementById("point-type").value;
	var text = type;
	point_coordinates = [lngLat.lng, lngLat.lat];
	if (text == "Race Marker") {
		race_marker_count++;
		if (race_marker_count == 1) {
			text = "Race Start";
			points.features.push({
				"type": 'Feature',
				"geometry": {
					"type": 'Point',
					"coordinates": point_coordinates
				},
				"properties": {
					"title": text,
					"icon": icon[type],
				}
			});
			map.getSource('points').setData(points);
		}
		else text = "Race Point " + (race_marker_count - 1);
		trace.features[0].geometry.coordinates.push(point_coordinates);
		map.getSource('trace').setData(trace);
	} else {
		points.features.push({
			"type": 'Feature',
			"geometry": {
				"type": 'Point',
				"coordinates": point_coordinates
			},
			"properties": {
				"icon": icon[type]
			}
		});
		map.getSource('points').setData(points);
	}
	save_points.push({ /* Data to be saved on database */
		"lat": lngLat.lat,
		"lng": lngLat.lng,
		"type": type
	});
}

function saveRoute() {
	$.post(
		".../api/postRacePoints",
		JSON.stringify({
			"name": document.getElementById("event-title").value,
			"date": document.getElementById("event-date").value,
			"description": document.getElementById("event-description").value,
			"points": save_points
		}),
		function (result) {
			document.location.reload(true);
		}
	);
}

function get_url_parameter(name, url) {
	if (!url) url = location.href;
	name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
	var regexS = "[\\?&]" + name + "=([^&#]*)";
	var regex = new RegExp(regexS);
	var results = regex.exec(url);
	return results == null ? null : results[1];
}

function openForm() {
	document.getElementById("admin_form").style.display = "block";
	document.getElementById("edit_form_div").style.display = "block";
	document.getElementById("event_details").style.display = "none";

	if (event_info != null) {
		document.getElementById("event-title").value = event_info.title
		document.getElementById("event-description").value = event_info.description
		document.getElementById("event-date").value = event_info.start
	}
	marker = new mapboxgl.Marker({ 	/* Single Dragger marker to determine position for points to be added */
		draggable: true, color: "#fff"
	})
		.setLngLat(starting_point)
		.addTo(map);
	lngLat = marker.getLngLat();
	marker.on('dragend', function () {
		lngLat = marker.getLngLat();
	});
	points = { /* Reset points*/
		"type": "FeatureCollection",
		"features": []
	};
	trace = {
		"type": "FeatureCollection",
		"features": [{
			"type": "Feature",
			"geometry": {
				"type": "LineString",
				"coordinates": []
			}
		}]
	};
	race_marker_count = 0;
	map.getSource('trace').setData(trace);
	map.getSource('points').setData(points);
}

function closeWithoutSaving() {
	document.location.reload(true);
}

function deleteEvent() {
	$.ajax({
		type: "GET",
		url: ".../api/deleteEvent?name=" + get_url_parameter("name"),
		success: function (result) {
			window.location.replace("index.html");
		}
	});
}
function medical(i) {
	var info = attendees[i].medical_info.split(',');
	alert(
		"Age: " + info[2] + "\n" +
		"Gender: " + info[3] + "\n" +
		"Height: " + info[4] + "\n" +
		"Weight: " + info[5] + "\n" +
		"Emergency Contact Name: " + info[6] + "\n" +
		"Emergency Contact Number: " + info[7] + "\n" +
		"Provider: " + info[8] + "\n" +
		"Allergies: " + info[9] + "\n" +
		"Medications: " + info[10] + "\n" +
		"High Blood Pressure: " + info[11] + "\n" +
		"Heart Disease: " + info[12] + "\n" +
		"Heart Attack: " + info[13] + "\n" +
		"Stroke: " + info[14] + "\n" +
		"Vascular Disease: " + info[15] + "\n" +
		"Neuromuscular Disease: " + info[16] + "\n" +
		"Kidney Renal Disease: " + info[17] + "\n" +
		"Bariatric Surgery: " + info[18] + "\n" +
		"Arthritis: " + info[19] + "\n" +
		"Cancer: " + info[20] + "\n" +
		"Congenital Disease: " + info[21] + "\n" +
		"Psychiatric Disorder: " + info[22] + "\n" +
		"Additional Information: " + info[23]
	);
}

function openInfo() {
	document.getElementById("info_form").style.display = "block";
	document.getElementById("event_details").style.display = "none";
	const form = document.getElementById("attendees");
	while (form.firstChild) {
		form.removeChild(form.firstChild);
	}
	$.ajax({
		type: "GET",
		url: ".../api/getEventAttendees?eventID=" + get_url_parameter("name"),
		success: function (result) {
			attendees = JSON.parse(result).members;
			for (var i = 0; i < attendees.length; i++) {
				$("#attendees").append(
					$.parseHTML(
						`<div class="field">
							<ul class="icons">
								<li>
									<p>` + attendees[i].firstName.replace(/^\w/, c => c.toUpperCase()) + " " + attendees[i].lastName.replace(/^\w/, c => c.toUpperCase()) + `: ` + new Date(attendees[i].checkIn).toLocaleTimeString() + `-` + new Date(attendees[i].checkOut).toLocaleTimeString() + `</p>
								</li>
								<li>
									<a href="mailto:` + attendees[i].email + `" class="icon solid style2 fa-envelope"><span class="label">Email</span></a>
								</li>
								<li>
									<a href="tel:` + attendees[i].phone + `" class="icon solid style2 fa-phone"><span class="label">Phone</span></a>
								</li>
								<li>
									<a onclick="medical(` + i + `)" class="icon solid style2 fa-id-card"><span class="label">Medical</span></a>
								</li>
							</ul>
						</div>`
					)
				);
			}
		}
	});
}
function closeInfo() {
	document.getElementById("info_form").style.display = "none";
	document.getElementById("event_details").style.display = "block";
}
function newEvent() {
	var name = prompt("Enter the new event name to get started:");
	if (name != null) {
		$.post(
			".../api/postRacePoints",
			JSON.stringify({
				"name": name,
				"date": document.getElementById("event-date").value,
				"description": "default description",
				"points": load_points
			}),
			function (result) {
				window.location.replace("map.html?name=" + name.toLowerCase().replace(/ /g, "-"));
			}
		);
	}
}

function register() {
	$.post(
		".../api/registerForEvent?googleID=" + profile.getId() + "&eventID=" + get_url_parameter("name"),
		JSON.stringify({}),
		function (result) {
		}
	);
	if (event_occuring) document.getElementById("checkIn").style.display = "block";
	else document.getElementById("unregister").style.display = "block";
	document.getElementById("register").style.display = "none";
}
function unregister() {
	$.post(
		".../api/unregisterForEvent?googleID=" + profile.getId() + "&eventID=" + get_url_parameter("name"),
		JSON.stringify({}),
		function (result) {
		}
	);
	document.getElementById("register").style.display = "block";
	document.getElementById("unregister").style.display = "none";
}
function checkIn() {
	$.post(
		".../api/checkIn?googleID=" + profile.getId() + "&eventID=" + get_url_parameter("name"),
		JSON.stringify({}),
		function (result) {
			checked_in = true;
			document.getElementById("event_registration").textContent = "Check Out";
		}
	);
	document.getElementById("checkOut").style.display = "block";
	document.getElementById("checkIn").style.display = "none";
}
function checkOut() {
	$.post(
		".../api/checkOut?googleID=" + profile.getId() + "&eventID=" + get_url_parameter("name"),
		JSON.stringify({}),
		function (result) {
			checked_in = false;
			document.getElementById("event_registration").textContent = "Check In";
		}
	);
	document.getElementById("finished").style.display = "block";
	document.getElementById("checkOut").style.display = "none";
}

var googleUser = {};
var startApp = function () {
	gapi.load('auth2', function () {
		auth2 = gapi.auth2.init({
			client_id: '....apps.googleusercontent.com',
			cookiepolicy: 'single_host_origin',
		});
		attachSignin(document.getElementById('customBtn'));
	});
};

function attachSignin(element) {
	auth2.attachClickHandler(element, {},
		function (googleUser) {
			profile = googleUser.getBasicProfile();
			$.ajax({
				url: ".../api/getEventMember" +
					"?googleID=" + profile.getId() +
					"&firstName=" + profile.getGivenName() +
					"&lastName=" + profile.getFamilyName() +
					"&email=" + profile.getEmail() +
					"&eventID=" + get_url_parameter("name"),
				success: function (result) {
					var event_member_data = JSON.parse(result);
					if (event_occuring) {
						if (event_member_data.has_registered == "0") {
							document.getElementById("register").style.display = "block";
						} else {
							if (event_member_data.checked_out == "") {
								if (event_member_data.checked_in == "") {
									document.getElementById("checkIn").style.display = "block";
								} else {
									document.getElementById("checkOut").style.display = "block";
								}
							}
							else {
								document.getElementById("finished").style.display = "block";
							}
						}
					}
					else {
						if (event_member_data.has_registered == "0") {
							document.getElementById("register").style.display = "block";
						} else {
							document.getElementById("unregister").style.display = "block";
						}
					}
					if (event_member_data.is_admin == "1") {
						document.getElementById("adminAction1").style.display = "block";
						document.getElementById("adminAction2").style.display = "block";
					}
					document.getElementById("event_registration").style.display = "none";
				}
			});
		}, function (error) {
			console.log(JSON.stringify(error, undefined, 2));
		});
}
startApp();

map.on('load', function () {
	map_loaded = true;
	if (map_content_loaded) populateMap();
});