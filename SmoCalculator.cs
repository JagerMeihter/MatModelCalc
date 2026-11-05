using System;
using System.Text;

namespace SmoCalc
{
    public static class SmoCalculator
    {
        private static string F(double v)
        {
            return v.ToString("F4");
        }

        // Вероятности состояний p0..p(n+m) для СМО с n каналами и m местами в очереди.
        // Из уравнений Колмогорова: w[k] = w[k-1] * ρ / min(k, n), затем нормировка.
        private static double[] Probabilities(int n, int m, double rho)
        {
            int size = n + m + 1;
            double[] p = new double[size];
            p[0] = 1.0;
            double sum = 1.0;
            for (int k = 1; k < size; k++)
            {
                p[k] = p[k - 1] * rho / Math.Min(k, n);
                sum += p[k];
            }
            for (int k = 0; k < size; k++) p[k] /= sum;
            return p;
        }

        // 1) СМО с отказами (n каналов, очереди нет) — формулы Эрланга
        public static string Loss(int n, double lambda, double mu, string unit)
        {
            double rho = lambda / mu;
            double[] p = Probabilities(n, 0, rho);

            double pRefuse = p[n];          // вероятность отказа
            double q = 1.0 - pRefuse;       // относительная пропускная способность
            double a = lambda * q;          // абсолютная пропускная способность
            double busy = rho * q;          // среднее число занятых каналов

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("СМО с отказами, n = " + n);
            sb.AppendLine("--------------------------------------");
            sb.AppendLine("λ = " + F(lambda) + " заявок/" + unit);
            sb.AppendLine("μ = " + F(mu) + " заявок/" + unit + "  (на один канал)");
            sb.AppendLine("ρ = λ/μ = " + F(rho));
            sb.AppendLine();
            sb.AppendLine("P0 (все каналы свободны)   = " + F(p[0]));
            sb.AppendLine("P отк (вероятность отказа) = " + F(pRefuse));
            sb.AppendLine("Q (отн. пропускная спос.)  = " + F(q));
            sb.AppendLine("A (абс. пропускная спос.)  = " + F(a) + " заявок/" + unit);
            sb.AppendLine("Среднее число занятых каналов = " + F(busy));
            sb.AppendLine("Доля обслуживаемых заявок   = " + F(q * 100) + " %");
            sb.AppendLine();
            sb.AppendLine("Вероятности состояний:");
            for (int k = 0; k <= n; k++)
                sb.AppendLine("  P" + k + " = " + F(p[k]));
            return sb.ToString();
        }

        // 2) СМО с неограниченной очередью (n каналов)
        public static string InfiniteQueue(int n, double lambda, double mu, int kMax, string unit)
        {
            double rho = lambda / mu;
            double psi = rho / n;
            if (psi >= 1.0)
                throw new ArgumentException("Очередь растёт неограниченно: ρ/n = " + F(psi) +
                                            " ≥ 1. Стационарного режима нет.");

            // w[k] = ρ^k / k!  для k = 0..n
            double[] w = new double[n + 1];
            w[0] = 1.0;
            for (int k = 1; k <= n; k++) w[k] = w[k - 1] * rho / k;

            double sum = 0.0;
            for (int k = 0; k < n; k++) sum += w[k];
            sum += w[n] / (1.0 - psi);
            double p0 = 1.0 / sum;

            double pWait = p0 * w[n] / (1.0 - psi);                  // вероятность ожидания (Эрланг C)
            double lq = p0 * w[n] * psi / ((1.0 - psi) * (1.0 - psi)); // средняя длина очереди
            double ls = lq + rho;                                      // среднее число заявок в системе
            double wq = lq / lambda;                                   // среднее время в очереди
            double ws = ls / lambda;                                   // среднее время в системе

            // вероятность, что в очереди не более kMax заявок = P0 + ... + P(n+kMax)
            double cum = 0.0;
            StringBuilder states = new StringBuilder();
            for (int i = 0; i <= n + kMax; i++)
            {
                double pi = (i <= n) ? w[i] * p0 : w[n] * Math.Pow(psi, i - n) * p0;
                cum += pi;
                states.AppendLine("  P" + i + " = " + F(pi));
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("СМО с неограниченной очередью, n = " + n);
            sb.AppendLine("--------------------------------------");
            sb.AppendLine("λ = " + F(lambda) + " заявок/" + unit);
            sb.AppendLine("μ = " + F(mu) + " заявок/" + unit + "  (на один канал)");
            sb.AppendLine("ρ = λ/μ = " + F(rho) + ",  ρ/n = " + F(psi));
            sb.AppendLine();
            sb.AppendLine("P0 (система пуста)        = " + F(p0));
            sb.AppendLine("P ож (заявка ждёт)        = " + F(pWait));
            sb.AppendLine("Lоч (средняя длина очереди) = " + F(lq));
            sb.AppendLine("Lсист (заявок в системе)    = " + F(ls));
            sb.AppendLine("Wоч (ср. время в очереди)  = " + F(wq) + " " + unit);
            sb.AppendLine("Wсист (ср. время в системе)= " + F(ws) + " " + unit);
            sb.AppendLine("Среднее число занятых каналов = " + F(rho));
            sb.AppendLine("Коэффициент загрузки ρ/n    = " + F(psi));
            sb.AppendLine("P отк = 0, Q = 1, A = λ = " + F(lambda));
            sb.AppendLine();
            sb.AppendLine("P(в очереди не более " + kMax + ") = " + F(cum));
            sb.AppendLine("(сумма P0..P" + (n + kMax) + ")");
            sb.AppendLine();
            sb.AppendLine("Вероятности состояний:");
            sb.Append(states.ToString());
            return sb.ToString();
        }

        // 3) СМО с ограниченной очередью (n каналов, m мест в очереди)
        public static string LimitedQueue(int n, int m, double lambda, double mu, string unit)
        {
            double rho = lambda / mu;
            double[] p = Probabilities(n, m, rho);

            double pRefuse = p[n + m];          // вероятность отказа (все места заняты)
            double q = 1.0 - pRefuse;
            double a = lambda * q;

            double lq = 0.0;                    // средняя длина очереди
            double busy = 0.0;                  // среднее число занятых каналов
            double pWait = 0.0;                 // вероятность того, что заявка встанет в очередь
            for (int k = 0; k <= n + m; k++)
            {
                if (k > n) lq += (k - n) * p[k];
                busy += Math.Min(k, n) * p[k];
                if (k >= n && k < n + m) pWait += p[k];
            }
            double ls = lq + busy;
            double wq = lq / a;                 // формула Литтла для принятых заявок
            double ws = ls / a;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("СМО с ограниченной очередью, n = " + n + ", m = " + m);
            sb.AppendLine("--------------------------------------");
            sb.AppendLine("λ = " + F(lambda) + " заявок/" + unit);
            sb.AppendLine("μ = " + F(mu) + " заявок/" + unit + "  (на один канал)");
            sb.AppendLine("ρ = λ/μ = " + F(rho));
            sb.AppendLine();
            sb.AppendLine("P0 (система пуста)        = " + F(p[0]));
            sb.AppendLine("P отк (вероятность отказа)= " + F(pRefuse));
            sb.AppendLine("Q (отн. пропускная спос.) = " + F(q));
            sb.AppendLine("A (абс. пропускная спос.) = " + F(a) + " заявок/" + unit);
            sb.AppendLine("P ож (заявка встаёт в очередь) = " + F(pWait));
            sb.AppendLine("Lоч (средняя длина очереди)    = " + F(lq));
            sb.AppendLine("Среднее число занятых каналов  = " + F(busy));
            sb.AppendLine("Lсист (заявок в системе)       = " + F(ls));
            sb.AppendLine("Wоч (ср. время в очереди)  = " + F(wq) + " " + unit);
            sb.AppendLine("Wсист (ср. время в системе)= " + F(ws) + " " + unit);
            sb.AppendLine();
            sb.AppendLine("Вероятности состояний:");
            for (int k = 0; k <= n + m; k++)
                sb.AppendLine("  P" + k + " = " + F(p[k]));
            return sb.ToString();
        }
    }
}