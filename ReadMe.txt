A pet project of mine. There are two solutions:

1) GetDailyTickers - this obtains a list of tickers present in the Tickers.txt, calls an API known as IEXCloud, and stores the data in my local SQL database.
I have it set to run every weekday at noon PST

2) StockTracker - this is a web app that does several different things:
	-Displays a line graph of SMA (Standard Moving Averages) and EMA (Exponential Moving Averages) for particular tickers
	the user can input how many days they wish to go back, and how many days they want the EMA/SMA to go. For instance
	the user can ask for a 17 day SMA spanning back 200 days, or a 376 day EMA spanning back 66 days. it doesn't matter.

	The functionality supports multiple line graphs, with the condition that all the line graphs go back the same amount of days.
	At the moment, I use a free API for this, as the request volume is not high (only I am using it)

	-I am working on a "Discount Tracker" which will return a list of all tickers that have fallen by X% in the last Y days
	I have a SQL script that does this, I just need to import it in.