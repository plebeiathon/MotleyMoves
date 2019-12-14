SELECT Pr.name, M.fullName, checkIn, checkOut FROM modestomovesdb.programattendees as Pa, modestomovesdb.programcalander as Pc, modestomovesdb.programs as Pr, modestomovesdb.members as M
	WHERE Pa.pcID = Pc.pcID AND Pc.prgramID = Pr.programID AND Pa.attendeeName = M.mermberID;
