import pytest
import requests_mock
from server import mcp, analyze_inflation_impact, get_exchange_rate

# 1. Test the Inflation Logic (The SARB Mandate Alignment)
def test_analyze_inflation_impact_logic():
    """Validates the calculation logic for price stability monitoring."""
    
    # Test case: Within SARB target (3-6%)
    within_target = analyze_inflation_impact(current_cpi=5.3, threshold=6.0)
    assert "WITHIN" in within_target 
    
    # Test case: Exceeding target
    above_target = analyze_inflation_impact(current_cpi=7.2, threshold=6.0)
    assert "ABOVE" in above_target 

# 2. Test the API Integration (The Data Layer Connection)
def test_get_exchange_rate_tool():
    """Ensures the MCP tool correctly parses data from the ASP.NET API."""
    
    with requests_mock.Mocker() as m:
        # Mocking the C# API response defined in your roadmap 
        m.get("https://your-api.azurewebsites.net/api/indicators", 
              json={"ZAR_USD": 18.45, "Inflation": 5.3})
        
        # Call the tool logic directly
        result = get_exchange_rate(base_currency="USD", target_currency="ZAR")
        
        assert "18.45" in result 
        assert "ZAR" in result 

# 3. Test for Agentic Tool Registration
def test_mcp_tool_registration():
    """Verifies that all roadmap tools are correctly registered with FastMCP."""
    registered_tools = [tool.name for tool in mcp.list_tools()]
    
    assert "get_exchange_rate" in registered_tools 
    assert "analyze_inflation_impact" in registered_tools 
    assert "summarize_economic_news" in registered_tools