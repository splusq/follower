import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import time

def send_test_email(subject, body):
    print(f"\nSending email with subject: {subject}")
    print("-" * 32)

    # Create message
    msg = MIMEMultipart()
    msg['Subject'] = subject
    msg['From'] = "sender@example.com"
    msg['To'] = "recipient@example.com"
    # Detect if body is HTML
    content_type = 'html' if body.strip().startswith('<!DOCTYPE') or body.strip().startswith('<html') else 'plain'
    msg.attach(MIMEText(body, content_type))

    # Connect to server
    try:
        with smtplib.SMTP('localhost', 2525) as server:
            server.set_debuglevel(1)  # Show SMTP conversation
            server.sendmail(msg['From'], msg['To'], msg.as_string())
            print("Email sent successfully\n")
    except Exception as e:
        print(f"Error sending email: {e}\n")

# Test 1: Email with contact information
send_test_email(
    "Meeting Request",
    """Hi,

Please contact John Doe:
Phone: (555) 123-4567
Email: john.doe@example.com
Address: 123 Main St, Suite 100, San Francisco, CA 94105

Best regards,
Alice"""
)

time.sleep(2)

# Test 2: Email with order details
send_test_email(
    "Order Confirmation #12345",
    """Dear Customer,

Thank you for your order!

Order Details:
- Order ID: 12345
- Product: Widget Pro
- Quantity: 2
- Price: $99.99 each
- Total: $199.98

Shipping Address:
Jane Smith
456 Oak Avenue
New York, NY 10001

Best regards,
The Sales Team"""
)

time.sleep(2)

# Test 3: Email with appointment details
send_test_email(
    "Appointment Confirmation",
    """Dear Mr. Johnson,

Your appointment has been scheduled:

Date: May 15, 2025
Time: 2:30 PM PST
Location: Virtual Meeting
Meeting ID: zoom.us/j/123456789
Duration: 60 minutes

Please let us know if you need to reschedule.

Best regards,
The Scheduling Team"""
)

time.sleep(2)

# Test 4: HTML email
send_test_email(
    "HTML Newsletter",
    """<!DOCTYPE html>
<html>
<head>
    <title>Monthly Newsletter</title>
</head>
<body>
    <h1>Monthly Newsletter</h1>
    
    <h2>Featured Article</h2>
    <p>We're excited to announce our new product line!</p>
    
    <h3>Key Features:</h3>
    <ul>
        <li>Enhanced Performance</li>
        <li>Better Battery Life</li>
        <li>Improved User Interface</li>
    </ul>
    
    <h2>Upcoming Events</h2>
    <table>
        <tr>
            <th>Date</th>
            <th>Event</th>
            <th>Location</th>
        </tr>
        <tr>
            <td>May 1, 2025</td>
            <td>Product Launch</td>
            <td>San Francisco</td>
        </tr>
        <tr>
            <td>May 15, 2025</td>
            <td>Developer Workshop</td>
            <td>Online</td>
        </tr>
    </table>
    
    <p>For more information, visit our <a href="https://example.com/news">news page</a>.</p>
    
    <blockquote>
        "Innovation is the key to success in technology." - Tech Leader
    </blockquote>
    
    <p><strong>Important:</strong> Don't forget to <em>register early</em> for the best pricing!</p>
</body>
</html>"""
)

print("All test emails sent!")
