
'''
This script shares a module with another script.  Does the module have 
same name space across both?
'''

import sys
import sharedmodule


LGlobal = ''

def setLGlobal(v):
	global LGlobal
	LGlobal = v

def getLGlobal():
	return LGlobal

def setMGlobal(v):
	sharedmodule.setMGlobal(v)

def getMGlobal():
	return sharedmodule.getMGlobal()


