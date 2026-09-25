metadata name = 'Type Definitions'
metadata description = 'This module defines the types used for shared resources.'
metadata owner = 'platform-engineers'

// ---------------- //
// TYPE DEFINITIONS //
// ---------------- //

@export()
@description('The type for a lock.')
type lockType = {
  @description('Optional. Specify the name of lock.')
  name: string?

  @description('Optional. The lock settings of the service.')
  kind: ('CanNotDelete' | 'ReadOnly' | 'None')

  @description('Optional. Notes about this lock.')
  notes: string?
}?
