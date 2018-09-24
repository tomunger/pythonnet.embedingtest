'''
This script throws an error.
'''

def makeError(obj):
	# Reference attribute on None.
	obj.nonexistent = 1


def invitationToError():
	# Make a function call so the stack has more than one frame.
	makeError(None)

