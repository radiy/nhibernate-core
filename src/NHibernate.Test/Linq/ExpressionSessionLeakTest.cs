using System;
using System.Linq;
using System.Threading;
using NHibernate.DomainModel.Northwind.Entities;
using NHibernate.Linq;
using NUnit.Framework;

namespace NHibernate.Test.Linq
{
	public class ExpressionSessionLeakTest : LinqTestCase
	{
		[Test]
		public void SessionGetsCollected()
		{
			var reference = DoLinqInSeparateSession();

			GC.Collect();

			Assert.That(reference.IsAlive, Is.False);
		}

		[Test]
		public void LeakObjectRef()
		{
			var checRef = DoLeakObjectRef();

			GC.Collect();
			Assert.IsFalse(checRef.IsAlive);
		}

		private WeakReference DoLeakObjectRef()
		{
			WeakReference checRef;
			using (var session2 = session.SessionFactory.OpenSession()) {
				var c1 = session2.Query<Customer>().First();
				checRef = new WeakReference(c1);
				session2.Query<Customer>().Count(c => c != c1);
			}
			return checRef;
		}

		private WeakReference DoLinqInSeparateSession()
		{
			using (var leakTest = session.SessionFactory.OpenSession())
			{
				// It appears linq expressions will (or might) contain a reference to the
				// IQueryable. At time of writing, linq expressions are helt within NhLinqExpression,
				// which in turn will be held in the query plan cache. Since the IQueryable will
				// be an NhQueryable, which holds a reference to the SessionImpl instance,
				// we will be leaking session instances.

				var query = leakTest.Query<Customer>().FirstOrDefault(t => t.CustomerId != "");

				return new WeakReference(leakTest, false);
			}
		}
	}
}
