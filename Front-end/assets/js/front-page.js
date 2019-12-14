// Populating front-page content with current events
$.ajax({
    url: ".../api/getCalendarEvents",
    success: function (result) {
        var today = new Date();
        var date = today.getFullYear() + '-' + (today.getMonth() + 1) + '-' + today.getDate();
        var maps = JSON.parse(result);
        var calendarEl = document.getElementById("calendar");
        var calendar = new FullCalendar.Calendar(calendarEl, {
            plugins: ["interaction", "dayGrid", "timeGrid", "list"],
            height: "parent",
            header: {
                left: "",
                center: "title",
                right: "prev,next today"
            },
            defaultView: "dayGridMonth",
            defaultDate: "2019-12-13",
            navLinks: false,
            editable: true,
            eventLimit: true,
            events: maps["events"]
        });
        calendar.render();
        mapboxgl.accessToken = "...";
        maps["events"].forEach(function (map) {
            var map_id = map.url.substring(map.url.lastIndexOf("=") + 1)
            var day_lookup = ["Sunday", " Monday", " Tuesday", " Wednesday", " Thursday", " Friday", " Saturday"];
            var month_lookup = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
            var then = new Date(map.start);
            then.setDate(then.getDate() + 1);

            $("#inner-events").append(
                $.parseHTML(
                    `<article style="height:18em;width:28em">
                        <div id="map`+ map_id + `" style="position:absolute;top: 0;bottom: 0;width: 100%;"></div>
                        <div class="caption">
                            <h3>` + map.title + `</h3>
                            <p>` + map.description + `</p>
                            <ul class="icons">
                                <li><a href="#events-calendar" class="icon fa-calendar smooth-scroll-middle" id="event_date"><span class="label">Event Date</span> ` + day_lookup[then.getDay()] + ", " + month_lookup[then.getMonth()] + " " + then.getDate() + ", " + then.getFullYear() + `</a></li>
                            </ul>
                            <ul class="actions fixed">
                                <li><a href="map.html?name=` + map_id + `"><span class="button small">More Info</span></a></li>
                            </ul>
                        </div>
                    </article>`
                )
            );
            var map = new mapboxgl.Map({
                container: "map" + map_id,
                style: "mapbox://styles/mapbox/dark-v10",
                center: [-120.996880, 37.639095], //Assume Centering on Modesto
                zoom: 8,
                interactive: false
            });
            map.on("load", function () {
                $.ajax({
                    url: ".../api/getRacePoints?name=" + map_id,
                    success: function (result) {
                        var load_points = JSON.parse(result).points;

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
                        map.flyTo({
                            center: [load_points[0].lng, load_points[0].lat],
                            zoom: 11,
                            bearing: 0,
                            speed: 2,
                            curve: 1
                        });
                        for (var i = 0; i < load_points.length; i++) {
                            if (load_points[i].type == "Race Marker") { /* Add line segment and update source */
                                trace.features[0].geometry.coordinates.push([load_points[i].lng, load_points[i].lat]);
                            }
                        }
                        map.getSource('trace').setData(trace);
                    }
                });
            });
        });
    }
});
function get_url_parameter(name, url) {
    if (!url) url = location.href;
    name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
    var regexS = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexS);
    var results = regex.exec(url);
    return results == null ? null : results[1];
}