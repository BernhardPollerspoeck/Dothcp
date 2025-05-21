# Proposed GitHub Issues

This document outlines suggested GitHub issues to track the implementation of missing features in the DHCP server project. Issues are organized by epics (major feature areas) and include individual tasks.

## Epic: Core DHCP Protocol Implementation

### High Priority Issues

1. **Implement HandleRequest Method**
   - Complete the `HandleRequest` method in `DhcpManagerGrain.cs`
   - Handle ACK responses for lease requests
   - Update client state tracking
   - Mark IPs as leased in the IP address manager

2. **Implement DHCP Lease Tracking**
   - Complete the `DhcpLease` class implementation
   - Implement proper lease time tracking
   - Add lease database maintenance

3. **Dynamic IP Address Management**
   - Fix subnet and broadcast address calculation
   - Improve IP allocation strategy
   - Implement IP conflict detection

4. **Implement DHCP NAK Responses**
   - Add support for rejecting invalid requests
   - Implement proper NAK message construction
   - Handle error scenarios appropriately

### Medium Priority Issues

5. **Complete DHCP Protocol Message Handling**
   - Add support for DECLINE messages
   - Implement RELEASE message handling
   - Add INFORM message handling

6. **Implement BOOTP Protocol Support**
   - Add basic BOOTP compatibility
   - Support legacy clients

7. **Advanced DHCP Options Support**
   - Add support for additional DHCP options
   - Implement option 82 for relay agents
   - Support vendor-specific options

## Epic: Blazor UI Implementation

### High Priority Issues

8. **Implement Dashboard Home Page**
   - Create server status display
   - Show lease utilization statistics
   - Display active lease count

9. **Make Settings Page Editable**
   - Convert display-only settings to editable fields
   - Implement save functionality
   - Add validation for settings values
   - Implement success/error feedback

10. **Create Active Leases View**
    - Implement table of current DHCP leases
    - Show lease details (IP, MAC, expiration)
    - Add basic filtering capabilities

### Medium Priority Issues

11. **Implement IP Reservation Management**
    - Create UI for managing static IP assignments
    - Add MAC-to-IP reservation functionality
    - Implement reservation validation

12. **Add Client History View**
    - Track client activity over time
    - Show lease history by client
    - Implement client search functionality

13. **Implement Settings Import/Export**
    - Add ability to save settings to file
    - Support importing settings
    - Add backup/restore functionality

### Lower Priority Issues

14. **Enhance UI Navigation and Layout**
    - Improve navigation structure
    - Create better organized menu
    - Make UI responsive for different screen sizes

15. **Add UI Themes and Customization**
    - Implement light/dark mode switch
    - Add customizable UI elements
    - Improve overall UI aesthetics

## Epic: Security and Authentication

### High Priority Issues

16. **Implement Basic Authentication**
    - Add login screen
    - Implement user authentication
    - Protect admin features

### Medium Priority Issues

17. **Add Role-Based Access Control**
    - Implement user roles
    - Create permission system
    - Restrict access based on permissions

18. **Implement Audit Logging**
    - Track configuration changes
    - Log administrative actions
    - Create audit trail viewer

## Epic: Network Infrastructure

### High Priority Issues

19. **Fix Gateway and Network Calculation**
    - Replace hardcoded values in network calculations
    - Implement proper subnet mask handling
    - Calculate broadcast address correctly

### Medium Priority Issues

20. **Add Multi-Subnet Support**
    - Support multiple DHCP scopes
    - Implement subnet selection logic
    - Add subnet-specific configuration

21. **Implement DHCP Relay Support**
    - Handle relayed DHCP messages
    - Support option 82 processing
    - Configure relay agent settings

## Epic: Deployment and Operations

### Medium Priority Issues

22. **Create Docker Containerization**
    - Create Dockerfile
    - Document container usage
    - Support volume mounts for persistence

23. **Implement Health Monitoring**
    - Add health check endpoints
    - Create basic monitoring UI
    - Implement system alerts

### Lower Priority Issues

24. **Create Deployment Documentation**
    - Write installation guide
    - Document system requirements
    - Create troubleshooting guide

## Epic: Testing and Quality Assurance

### High Priority Issues

25. **Implement Core Unit Tests**
    - Add tests for DHCP message handling
    - Test IP allocation logic
    - Create lease management tests

### Medium Priority Issues

26. **Add Integration Tests**
    - Test end-to-end DHCP workflows
    - Create network simulation tests
    - Test multi-client scenarios

27. **Implement UI Tests**
    - Test Blazor components
    - Create UI interaction tests
    - Validate form submissions

## Implementation Plan

### Phase 1: Core Functionality
Focus on issues #1-4, #8-10, #16, #19, #25

### Phase 2: Feature Enhancement
Address issues #5-7, #11-13, #17-18, #20-21, #26

### Phase 3: Polish and Completion
Complete issues #14-15, #22-24, #27