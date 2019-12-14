SELECT m.fullName, Ev.name, checkIn, checkOut FROM modestomovesdb.eventattendees as Ea, modestomovesdb.events as Ev, modestomovesdb.members as M
	WHERE Ea.eventID = Ev.eventID AND Ea.attendeeName = M.mermberID;
