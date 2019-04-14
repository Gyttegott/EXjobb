
				# r = s.scan(/(?<=name=\").*?(?=\";)/)
				# v = s.scan(/(?<=value=\").*?(?=\"$|\"\n)/)
				# c = s.scan(/(?<=CommandInvocation\().*?(?=\))/)
				# r = r.flatten
				#v = v.flatten
				# c = c.flatten
				# x = r.zip(v).map {|par, val| '-' + par + ' ' + val}			
				# event.set('powershell_parameters', x)
				# event.set('powershell_command', c)
				
def filter(event)
	retval = Array.new
	param3 = event.get('[winlog][event_data][param3]')
	param2 = event.get('[winlog][event_data][param2]')
	scriptname = param2[/(?<=(\n|\t)ScriptName=).*?(?=\n)/]
	hostapp = param2[/(?<=(\n|\t)HostApplication=).*?(?=\n)/]
	event.set('[powershell][script_name]', scriptname)
	event.set('[powershell][host_application]', hostapp)
	event.set('[powershell][main_command]', event.get('[winlog][event_data][param1]'))
	event.remove('[winlog][event_data]')
	
	commands = param3.split('CommandInvocation(')
	if commands.length > 1
		for i in 1..commands.length-1
			# Find the current command being executed
			name = commands[i][/^.*?(?=\))/]
			event.set('[powershell][command]', name)
			# Skip commands not of interest
			next if ['ForEach-Object', 'Out-Default', 'Set-StrictMode', 'New-Object', 'Add-Member'].include? name
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