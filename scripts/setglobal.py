'''
This script depends on the embeding application to set global
'Logger' 
'''

# Global logger is not defined in this script - it is set by the
# embeding application.

Logger.Write("This script got compiled")

def writeGlobalLogger():
	'''Read the global 'Count', which is set by the caller.'''
	global Count
	Logger.Write("Count is {}".format(Count))
