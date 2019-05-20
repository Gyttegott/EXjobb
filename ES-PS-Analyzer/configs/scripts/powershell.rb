def filter(event)
	retval = Array.new
	payload = event.get('[winlog][event_data][param3]')
	commands = payload.split('CommandInvocation(')
	
	if commands.length > 1
		for i in 1..commands.length-1
			# Find the current command being executed
			name = commands[i][/^.*?(?=\))/]
			event.set('[powershell][command]', name)
			# Skip commands not of interest
			next if ['ForEach-Object', 'Out-Default', 'Set-StrictMode', 'Add-Member', 'Format-Table', 'PSConsoleHostReadline', 'Write-Host'].include? name
			# Skip .exe calls in module logging
			next if name.match(/.*\.exe/)
			# find all parameter names
			r = commands[i].scan(/(?<=name=\").*?(?=\";)/)
			# find all parameter values
			v = commands[i].scan(/(?<=value=\").*?(?=\"$|\"\n)/)
			# merge parameters with their values
			if r.length > 0
				x = r.zip(v).map {|par, val| '-' + par + ' ' + (val == nil ? '' : val)}
				event.set('[powershell][parameters]', x)
			end
			retval.push(event.clone)
		end
	end

	return retval

end