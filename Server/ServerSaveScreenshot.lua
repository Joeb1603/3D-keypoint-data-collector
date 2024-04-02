RegisterNetEvent('saveImg')
AddEventHandler('saveImg', function(saveDir, id)

	local src = source -- get the client that triggered the event

	exports['screenshot-basic']:requestClientScreenshot(GetPlayers()[1], {
		fileName = saveDir.."images\\"..id..".jpg"
	}, function(err, data)
		print(saveDir.."images\\"..id..".jpg")

		TriggerClientEvent("saveMetadata", src)

	end)
	
end)
