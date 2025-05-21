# Fix Gateway and Network Calculation

**Priority:** High

## Description

Currently, gateway addresses and network calculations use hardcoded values in the DHCP server code. This needs to be fixed to allow proper configuration of network parameters and correct calculation of subnet values.

## Implementation Details

1. Replace hardcoded network values:
   - Remove hardcoded gateway address in `DhcpMessage.ToData()` method
   - Use configured gateway from settings
   - Add proper validation for gateway configuration
   - Create fallback mechanisms for missing values

2. Implement proper subnet mask handling:
   - Replace hardcoded subnet mask values
   - Calculate network and host portions correctly
   - Validate subnet mask format and values
   - Handle CIDR notation conversion

3. Calculate broadcast address correctly:
   - Implement broadcast address calculation based on network address and subnet mask
   - Ensure broadcast address is never assigned to clients
   - Add validation for broadcast address consistency
   - Fix related network range calculations

## Testing Criteria

- Verify gateway address is correctly set from configuration
- Test subnet mask calculation with different configurations
- Confirm broadcast address is correctly calculated
- Test network range determination
- Verify first and last usable IP calculation
- Confirm compatibility with different subnet sizes