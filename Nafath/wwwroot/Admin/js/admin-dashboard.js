(function () {
    function readChartData() {
        var chartDataElement = document.getElementById('dashboard-chart-data');
        if (!chartDataElement) {
            return null;
        }

        try {
            return JSON.parse(chartDataElement.textContent || '{}');
        } catch (error) {
            return null;
        }
    }

    function createRevenueChart(data) {
        var canvas = document.getElementById('dashboard-revenue-chart');
        if (!canvas) {
            return;
        }

        new Chart(canvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: data.monthlyLabels || [],
                datasets: [{
                    label: 'Revenue',
                    backgroundColor: 'rgba(60, 141, 188, 0.18)',
                    borderColor: 'rgba(60, 141, 188, 0.95)',
                    pointBackgroundColor: 'rgba(60, 141, 188, 1)',
                    pointBorderColor: '#fff',
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    borderWidth: 2,
                    fill: true,
                    data: data.monthlyRevenue || []
                }]
            },
            options: {
                maintainAspectRatio: false,
                responsive: true,
                legend: { display: false },
                scales: {
                    xAxes: [{
                        gridLines: { display: false }
                    }],
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        },
                        gridLines: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    }]
                },
                tooltips: {
                    callbacks: {
                        label: function (tooltipItem) {
                            return Number(tooltipItem.yLabel || 0).toLocaleString() + ' ر.س';
                        }
                    }
                }
            }
        });
    }

    function createStatusChart(data) {
        var canvas = document.getElementById('dashboard-status-chart');
        var labels = data.orderStatusLabels || [];
        var counts = data.orderStatusCounts || [];

        if (!canvas || !labels.length) {
            return;
        }

        new Chart(canvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: counts,
                    backgroundColor: ['#f39c12', '#17a2b8', '#28a745', '#dc3545', '#6c757d'],
                    borderWidth: 1
                }]
            },
            options: {
                maintainAspectRatio: false,
                responsive: true,
                legend: { display: false }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        if (typeof Chart === 'undefined') {
            return;
        }

        var data = readChartData();
        if (!data) {
            return;
        }

        createRevenueChart(data);
        createStatusChart(data);
    });
})();
