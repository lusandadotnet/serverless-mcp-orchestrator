from mcp.server.fastmcp import FastMCP
import httpx
import os
import logging

# 1. Initialize FastMCP

mcp = FastMCP("ZAR-Flow")

# 2. Configure Logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("mcp-zar-flow")

# 3. Environment Setup
API_URL = os.getenv("API_BASE_URL")

@mcp.tool()
async def get_exchange_rate(base_currency: str = "USD", target_currency: str = "ZAR") -> str:
    """Fetches the latest ZAR exchange rates from the Azure-hosted C# API."""
    if not API_URL:
        logger.error("API_BASE_URL environment variable is missing!")
        return "Error: System configuration issue. API URL not found."

    logger.info(f"Fetching rates for {base_currency}/{target_currency} from {API_URL}")

    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            # Calling your deployed C# API endpoint
            response = await client.get(f"{API_URL}/api/indicators")
            response.raise_for_status()
            data = response.json()
            
           
            rate = data.get("zar_USD") or data.get("ZAR_USD")
            status = data.get("status", "Unknown")

            return (f"--- ZAR-Flow Live Data ---\n"
                    f"Pair: {base_currency}/{target_currency}\n"
                    f"Rate: {rate}\n"
                    f"Source: {status}")

    except httpx.HTTPStatusError as e:
        logger.error(f"API Error: {e.response.status_code}")
        return f"Error: The C# API returned a status error: {e.response.status_code}"
    except Exception as e:
        logger.error(f"Unexpected connection error: {str(e)}")
        return f"Error: Could not connect to the database API. Details: {str(e)}"

@mcp.tool()
def analyze_inflation_impact(current_cpi: float, threshold: float = 6.0) -> str:
    """Calculates whether inflation exceeds SARB targets (3-6%)."""
    logger.info(f"Analyzing CPI: {current_cpi} against threshold: {threshold}")
    
    if current_cpi > threshold:
        return (f"ALERT: Current CPI ({current_cpi}%) is ABOVE the SARB upper threshold "
                f"of {threshold}%. This may trigger a hawkish interest rate response.")
    
    if current_cpi < 3.0:
        return f"⚠️ NOTE: Inflation ({current_cpi}%) is BELOW the 3% target range."
        
    return f"STABLE: Inflation ({current_cpi}%) is within the SARB mandate range."

@mcp.tool()
def summarize_economic_news(query_topic: str) -> str:
    """Scaffolding for summarizing recent South African financial news."""
    # This is a placeholder for your BeautifulSoup/Scraping logic
    logger.info(f"News query received for: {query_topic}")
    return f"Summary: Economic sentiment regarding '{query_topic}' remains cautiously optimistic pending the next MPC meeting."

if __name__ == "__main__":
    mcp.run()