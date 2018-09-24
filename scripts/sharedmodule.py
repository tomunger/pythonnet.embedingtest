'''
A shared module with a global.  


'''
import sys


MGlobal = "Module Global"



def setMGlobal(v):
	global MGlobal
	MGlobal = v

def getMGlobal():
	return MGlobal

